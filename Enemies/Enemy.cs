using Godot;
using System;

public partial class Enemy : CharacterBody3D
{
    [Export] public float maxHealth = 100f;
    [Export] public float attackCooldown = 2f;
    [Export] public float attackRange = 25f;
    [Export] public int pointValue = 10;
    [Export] public Node3D visualRoot;
    [Export] public Node3D projectileSpawn;
    [Export] public PackedScene projectileScene;
    [Export] public AnimationPlayer animPlayer;
    private NetworkCore networkCore;
    [Export] public NetID netId;
    public CampController camp = null;
    private float currentHealth;
    private float attackTimer = 0f;
    private FPSController target;
    private NodePath targetPath = new NodePath();
    private bool isInitialized = false;

    public override void _Ready()
    {
        if (!GenericCore.Instance.IsServer)
            return;
        if (animPlayer != null)
        {
            animPlayer.AnimationFinished += OnAnimationFinished;
        }
        if (GenericCore.Instance.IsServer)
        {
            networkCore = GetTree().GetFirstNodeInGroup("PlayerSpawn") as NetworkCore;

            if (networkCore == null)
            {
                GD.PrintErr("NetworkCore not found!");
            }
        }
    }

    public void Initialize()
    {
        if (!GenericCore.Instance.IsServer)
            return;
        PlayAnim("Idle");

        currentHealth = maxHealth;
        attackTimer = 0f;
        attackCooldown += (float)GD.RandRange(0f, 1f);
        target = null;
        targetPath = new NodePath();

        isInitialized = true;

    }

    public override void _PhysicsProcess(double delta)
    {
        if (!isInitialized || !GenericCore.Instance.IsServer)
            return;

        if (target == null && !targetPath.IsEmpty && HasNode(targetPath))
        {
            target = GetNode<FPSController>(targetPath);
        }
        if(target == null)
            return;
        if (!IsInstanceValid(target))
        {
            ClearTarget();
            return;
        }
        float distance = GlobalPosition.DistanceTo(target.GlobalPosition);
        if (distance > attackRange)
        {
            ClearTarget();
            return;
        }
        FaceTarget(target.GlobalPosition);
        attackTimer += (float)delta;
        if (attackTimer >= attackCooldown)
        {
            attackTimer = 0f;
            PlayAnim("Ranged");
        }
    }
    public void SetTarget(FPSController player)
    {
        if (!GenericCore.Instance.IsServer)
            return;
        if (player == null || !IsInstanceValid(player))
            return;
        
        target = player;
        targetPath = player.GetPath();
        attackTimer = 0f;

        PlayAnim("Ranged");
    }
    public void ClearTarget()
    {
        if (!GenericCore.Instance.IsServer)
            return;
        target = null;
        targetPath = new NodePath();
        attackTimer = 0f;

        PlayAnim("Idle");
    }
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
        if (!GenericCore.Instance.IsServer)
            return;
        if (player == null || !IsInstanceValid(player))
            return;

        if (target != null)
            return;

        SetTarget(player);
        if (camp != null)
        {
            camp.SetTarget(player);
        }
    }
    private void FaceTarget(Vector3 targetPos)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        targetPos.Y = GlobalPosition.Y;

        Vector3 dir = (targetPos - GlobalPosition).Normalized();
        float angle = Mathf.Atan2(dir.X, dir.Z);

        Rpc(nameof(SetRotationClient), angle);
    }
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SetRotationClient(float angle)
    {
        if (visualRoot != null)
        {
            visualRoot.Rotation = new Vector3(0, angle, 0);
        }
        else
        {
            Rotation = new Vector3(0, angle + Mathf.Pi, 0);
        }
    }
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
        Basis basis = Basis.LookingAt(dir, Vector3.Up);
        Quaternion rotation = basis.GetRotationQuaternion();
        networkCore.NetCreateObject(3, spawnPos, rotation, owner: netId.OwnerId);
    }

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