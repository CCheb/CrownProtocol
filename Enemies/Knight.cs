using Godot;
using System;

public partial class Knight : CharacterBody3D
{
    [Export] public float speed = 1.0f;
    [Export] public float chase = 0.001f; 
    [Export] public AnimationPlayer animPlayer;
    [Export] public NavigationAgent3D nav;
    [Export] public Area3D hitbox;
    [Export] public Vector3 sharedVelocity;
    [Export] public float patrolRadius = 2f;
    public NetworkPlayer player;
    public Random rand = new Random();
    private bool inChase = false;
    public Vector3 startPosition;
    public override void _Ready()
    {
        base._Ready();

        nav.MaxSpeed = 5.0f;

        hitbox.BodyEntered += OnSightEntered;
        startPosition = this.GlobalPosition;
        SlowStart();
    }

    public void SlowStart()
    {
        animPlayer.Play("Idle");
        newDest();
    }

    public override void _PhysicsProcess(double delta)
    {
        // ---------------- CLIENT ----------------
        if (!GenericCore.Instance.IsServer)
        {
            // Only handle animations on client
            if (sharedVelocity.Length() > 0.1f)
            {
                if (animPlayer.CurrentAnimation != "Walk")
                    animPlayer.Play("Walk");
            }
            else
            {
                if (animPlayer.CurrentAnimation != "Idle")
                    animPlayer.Play("Idle");
            }
            return;
        }

        // ---------------- SERVER LOGIC ----------------

        // Find closest player
        var players = GetTree().GetNodesInGroup("Player");
        NetworkPlayer closestPlayer = null;
        float closestDist = float.MaxValue;

        foreach (Node p in players)
        {
            if (p is not NetworkPlayer body)
                continue;

            float dist = GlobalPosition.DistanceTo(body.GlobalPosition);

            if (dist < closestDist)
            {
                closestDist = dist;
                closestPlayer = body;
            }
        }

        player = closestPlayer;

        // ---------------- TARGETING ----------------
        if (player != null)
        {
            float distToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);

            if (distToPlayer <= chase)
            {
                if (!inChase)
                    inChase = true;

                // 🔥 Only update target when needed (prevents jitter)
                if (nav.TargetPosition.DistanceTo(player.GlobalPosition) > 1.0f)
                    nav.TargetPosition = player.GlobalPosition;
            }
            else
            {
                if (inChase)
                    inChase = false;

                // Patrol when not chasing
                if (nav.DistanceToTarget() < 1.0f)
                    newDest();
            }
        }

        // ---------------- NAVIGATION ----------------
        Vector3 nextPos = nav.GetNextPathPosition();

        Vector3 toNext = nextPos - GlobalPosition;
        toNext.Y = 0;

        Vector3 dir = toNext.Length() > 0.1f ? toNext.Normalized() : Vector3.Zero;

        // Rotate toward movement direction
        if (dir.Length() > 0.1f)
        {
            LookAt(GlobalPosition + dir, Vector3.Up);
        }

        // ---------------- GRAVITY ----------------
        float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

        if (!IsOnFloor())
        {
            Velocity = new Vector3(
                Velocity.X,
                Velocity.Y - gravity * (float)delta,
                Velocity.Z
            );
        }
        else
        {
            Velocity = new Vector3(Velocity.X, 0, Velocity.Z);
        }

        // ---------------- MOVEMENT ----------------
        Vector3 horizontal = new Vector3(dir.X, 0, dir.Z) * speed;

        Velocity = new Vector3(
            horizontal.X,
            Velocity.Y,
            horizontal.Z
        );

        sharedVelocity = Velocity;

        // ---------------- ANIMATION (SERVER SIDE OPTIONAL) ----------------
        if (sharedVelocity.Length() > 0.1f)
        {
            if (animPlayer.CurrentAnimation != "Walk")
                animPlayer.Play("Walk");
        }

        MoveAndSlide();
    }

    public void newDest()
    {
        if (GenericCore.Instance.IsServer)
        {
            Vector3 randomOffset = new Vector3(
            (float)GD.RandRange(-patrolRadius, patrolRadius),
            0,
            (float)GD.RandRange(-patrolRadius, patrolRadius)
            );

            Vector3 target = GlobalPosition + randomOffset;
            nav.TargetPosition = target;
        }
    }

    public void OnSightEntered(Node body)
    {
        if (GenericCore.Instance.IsServer)
        {
            if (body is RigidBody3D && !body.IsInGroup("Enemy"))
            {
                Respawn();
            }

            if (body.IsInGroup("Player"))
            {
                animPlayer.Play("Attack");
            }
        }
        if (!GenericCore.Instance.IsServer)
        {
            if (body.IsInGroup("Player"))
            {
                animPlayer.Play("Attack");
            }
        }
    }

    public void Respawn()
    {
        this.GlobalPosition = this.startPosition;
    }
}
