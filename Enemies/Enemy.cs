using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    // ---------------- STATS ----------------
    [Export] public float maxHealth = 100f;
    [Export] public float attackCooldown = 2f;
    [Export] public float attackRange = 25f;
    [Export] public int pointValue = 10;

    // ---------------- NODES ----------------
    [Export] public Node3D visualRoot; // rotate this, not whole body
    [Export] public Node3D projectileSpawn;
    [Export] public PackedScene projectileScene;
    [Export] public AnimationPlayer animPlayer;
    private NetworkCore networkCore;
    [Export] public NetID netId;

    // ---------------- CAMP ----------------
    public CampController camp;

    // ---------------- STATE ----------------
    private float currentHealth;
    private float attackTimer = 0f;

    private FPSController target;
    private NodePath targetPath = new NodePath();

    private bool isInitialized = false;

    // ---------------- READY ----------------
    public override void _Ready()
    {
        // Only server runs logic
        if (!GenericCore.Instance.IsServer)
        {
            SetPhysicsProcess(false);
        }
        if (animPlayer != null)
        {
            animPlayer.AnimationFinished += OnAnimationFinished;
        }
        if (GenericCore.Instance.IsServer)
        {
            networkCore = GetTree().Root.GetNode<NetworkCore>("GameManager/MultiplayerSpawner");

            if (networkCore == null)
            {
                GD.PrintErr("NetworkCore not found!");
            }
        }
    }

    // ---------------- INIT ----------------
    public void Initialize(CampController campRef)
    {
        camp = campRef;

        currentHealth = maxHealth;
        attackTimer = 0f;

        target = null;
        targetPath = new NodePath();

        isInitialized = true;

        PlayAnim("Idle");
    }

    // ---------------- MAIN LOOP ----------------
    public override void _PhysicsProcess(double delta)
    {
        if (!isInitialized || !GenericCore.Instance.IsServer)
            return;

        // Rebuild target if needed
        if (target == null && !targetPath.IsEmpty && HasNode(targetPath))
        {
            target = GetNode<FPSController>(targetPath);
        }

        // Validate target
        if (target == null || !IsInstanceValid(target))
        {
            ClearTarget();
            return;
        }

        float distance = GlobalPosition.DistanceTo(target.GlobalPosition);

        // Lose aggro
        if (distance > attackRange)
        {
            ClearTarget();
            return;
        }

        // Face target
        FaceTarget(target.GlobalPosition);

        // Attack loop
        attackTimer += (float)delta;

        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            ShootProjectile();
        }
    }

    // ---------------- TARGETING ----------------
    public void SetTarget(FPSController player)
    {
        if (player == null || !IsInstanceValid(player))
            return;

        target = player;
        targetPath = player.GetPath();
        attackTimer = 0f;

        PlayAnim("Ranged");
    }

    public void ClearTarget()
    {
        target = null;
        targetPath = new NodePath();
        attackTimer = 0f;

        PlayAnim("Idle");
    }

    // Called by Area3D
    public void OnBodyEntered(Node3D body)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        Node current = body;

        while (current != null)
        {
            if (current is FPSController player)
            {
                PlayerSpotted(player);
                return;
            }

            current = current.GetParent();
        }
    }

    public void PlayerSpotted(FPSController player)
    {
        if (player == null || !IsInstanceValid(player))
            return;

        if (target != null)
            return;

        SetTarget(player);

        // 🔥 SHARE AGGRO WITH CAMP
        if (camp != null)
        {
            camp.SetTarget(player);
        }
    }

    // ---------------- ROTATION ----------------
    private void FaceTarget(Vector3 targetPos)
    {
        targetPos.Y = GlobalPosition.Y;

        if (visualRoot != null)
        {
            visualRoot.LookAt(targetPos, Vector3.Up);
            visualRoot.RotateY(Mathf.Pi); // fix flipped model
        }
        else
        {
            LookAt(targetPos, Vector3.Up);
            RotateY(Mathf.Pi);
        }
    }

    // ---------------- COMBAT ----------------
    private void ShootProjectile()
    {
        if (!GenericCore.Instance.IsServer)
            return;

        if (target == null || !IsInstanceValid(target))
            return;

        Vector3 spawnPos = projectileSpawn != null
            ? projectileSpawn.GlobalPosition
            : GlobalPosition;

        Vector3 dir = (target.GlobalPosition - spawnPos).Normalized();

        // 🔥 Convert direction → rotation
        Basis basis = Basis.LookingAt(dir, Vector3.Up);
        Quaternion rotation = basis.GetRotationQuaternion();

        networkCore.NetCreateObject(3, spawnPos, rotation);

        PlayAnim("Ranged");
    }

    // ---------------- DAMAGE ----------------
    public void TakeDamage(float amount)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        currentHealth -= amount;

        PlayAnim("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private async void Die()
    {
        if (!GenericCore.Instance.IsServer)
            return;

        PlayAnim("Death");

        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");

        if (camp != null)
        {
            camp.OnEnemyDied(this);
        }

        DestroySelf();
    }

    private void DestroySelf()
    {
        if (!GenericCore.Instance.IsServer)
            return;

        if (networkCore == null)
        {
            GD.PrintErr("No NetworkCore on projectile!");
            return;
        }

        if (netId != null)
        {
            networkCore.NetDestroyObject(netId);
        }
        else
        {
            GD.PrintErr("Projectile missing NetID!");
        }
    }

    // ---------------- ANIMATION ----------------
    private void PlayAnim(string name)
    {
        if (GenericCore.Instance.IsServer)
        {
            Rpc(nameof(PlayAnimClient), name);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PlayAnimClient(string name)
    {
        if (animPlayer == null)
            return;

        if (animPlayer.CurrentAnimation == name)
            return;

        animPlayer.Play(name);
    }

    private void OnAnimationFinished(StringName animName)
    {
        if (animName == "Ranged")
        {
            PlayAnim("Idle");
        }
    }
}