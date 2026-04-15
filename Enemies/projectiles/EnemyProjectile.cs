using Godot;
using System;

public partial class EnemyProjectile : CharacterBody3D
{
    // ---------------- SETTINGS ----------------
    [Export] public float speed = 20f;
    [Export] public float damage = 10f;
    [Export] public float lifetime = 5f;

    // ---------------- STATE ----------------
    private float lifeTimer = 0f;
    private NetworkCore networkCore;
    [Export] public NetID netId;
    // ---------------- READY ----------------
    public override void _Ready()
    {
        if (!GenericCore.Instance.IsServer)
        {
            SetPhysicsProcess(false);
        }
        if (GenericCore.Instance.IsServer)
        {
            networkCore = GetTree().Root.GetNode<NetworkCore>("GameManager/MultiplayerSpawner");

            if (networkCore == null)
            {
                GD.PrintErr("NetworkCore not found in projectile!");
            }
        }
    }

    // ---------------- MAIN LOOP ----------------
    public override void _PhysicsProcess(double delta)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        // 🔥 Forward direction from rotation
        Vector3 forward = -GlobalTransform.Basis.Z;

        Velocity = forward * speed;
        MoveAndSlide();

        lifeTimer += (float)delta;
        if (lifeTimer >= lifetime)
        {
            DestroySelf();
            return;
        }

        for (int i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            var collider = collision.GetCollider() as Node;

            if (collider == null)
                continue;

            HandleHit(collider);
            return;
        }
    }

    // ---------------- HIT LOGIC ----------------
    private void HandleHit(Node collider)
    {
        Node current = collider;

        while (current != null)
        {
            // 🔥 HIT PLAYER
            if (current is FPSController player)
            {
                DealDamage(player);
                DestroySelf();
                return;
            }

            current = current.GetParent();
        }

        // Hit something else → destroy
        DestroySelf();
    }

    // ---------------- DAMAGE ----------------
    private void DealDamage(FPSController player)
    {
        if (!GenericCore.Instance.IsServer)
            return;

        if (!IsInstanceValid(player))
            return;

        // 🔥 CALL PLAYER DAMAGE (YOU NEED THIS FUNCTION)
        if (player.HasMethod("TakeDamage"))
        {
            player.Call("TakeDamage", damage);
        }
        else
        {
            GD.Print("⚠ Player missing TakeDamage()");
        }
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
}