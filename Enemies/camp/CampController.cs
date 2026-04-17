using Godot;
using System;
using System.Collections.Generic;

public partial class CampController : Area3D
{
    [Export] public bool zoneActive = false;

    [Export] public float respawnTime = 30f;
    private float respawnTimer = 0f;
    public float aggroRange = 30f;
    [Export] public Godot.Collections.Array<Node3D> spawnPoints;
    private NetworkCore networkCore;
    public List<Enemy> activeEnemies = new List<Enemy>();
    
    public FPSController currentTarget;

    public override async void _Ready()
    {
        if (!GenericCore.Instance.IsServer)
            return;

        networkCore = GetTree().GetFirstNodeInGroup("PlayerSpawn") as NetworkCore;

        if (networkCore == null)
        {
            GD.PrintErr("NetworkCore NOT FOUND in CampController!");
            return;
        }

        networkCore.ObjectSpawned += OnObjectSpawned;
        await ToSignal(GetTree().CreateTimer(GD.RandRange(0.1f, 1.0f)), "timeout");
        SpawnCamp();
    }
    public override void _PhysicsProcess(double delta)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        if (activeEnemies.Count == 0 && !zoneActive)
        {
            respawnTimer += (float)delta;

            if (respawnTimer >= respawnTime)
            {
                SpawnCamp();
                respawnTimer = 0f;
            }
        }
        CheckTargetValid();
    }
    public void CheckTargetValid()
    {
        if (currentTarget == null)
            return;

        if (!IsInstanceValid(currentTarget))
        {
            ClearTarget();
            return;
        }

        if (GlobalPosition.DistanceTo(currentTarget.GlobalPosition) > aggroRange)
        {
            ClearTarget();
        }
    }

    public void SpawnCamp()
    {
        if (!GenericCore.Instance.IsServer)
            return;
        if (activeEnemies.Count > 0)
            return;
        if (networkCore == null)
        {
            GD.PrintErr("NetworkCore is null!");
            return;
        }
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            var point = spawnPoints[i];

            networkCore.NetCreateObject(2, point.GlobalPosition, Quaternion.Identity, camp: this);
        }
    }

    public void SetTarget(FPSController player)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        currentTarget = player;

        foreach (var enemy in activeEnemies)
        {
            enemy.SetTarget(player);
        }
    }

    public void OnEnemyDied(Enemy enemy)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        activeEnemies.Remove(enemy);

        if (activeEnemies.Count == 0)
        {
            SpawnDrop();
        }
    }

    public void ClearTarget()
    {
        if (!GenericCore.Instance.IsServer)
            return;

        currentTarget = null;

        foreach (var enemy in activeEnemies)
        {
            enemy.ClearTarget();
        }
    }

    public void SpawnDrop()
    {
        // spawn drop
    }

    public void _on_body_entered(Node3D body)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        if (body is FPSController player)
        {
            SetTarget(player);
        }
    }
    private void OnObjectSpawned(Enemy node)
    {
        if (node is Enemy enemy)
        {
            if (enemy.camp == this)
            {
                enemy.Initialize();
                activeEnemies.Add(enemy);
            }
        }
    }
}