using Godot;
using System;

public partial class EnemyProjectile : CharacterBody3D
{
    [Export] public float speed = 20f;
    [Export] public float damage = 5f;
    [Export] public float lifetime = 5f;
    private float lifeTimer = 0f;
    private NetworkCore networkCore;
    [Export] public long ownerId;
    public int bulletId;
    public override void _Ready()
    {
        if (!GenericCore.Instance.IsServer)
        {
            SetPhysicsProcess(false);
        }
        if (GenericCore.Instance.IsServer)
        {
            networkCore = GetTree().GetFirstNodeInGroup("PlayerSpawn") as NetworkCore;
            if (networkCore == null)
            {
                GD.PrintErr("NetworkCore not found in projectile!");
            }
        }
    }

   public override void _PhysicsProcess(double delta)
    {
        Vector3 motion = -GlobalTransform.Basis.Z * speed * (float)delta;
        if (GenericCore.Instance.IsServer)
        {
            // lifetime
            lifeTimer += (float)delta;
            if (lifeTimer >= lifetime)
            {
                DestroySelf();
                return;
            }

            var collision = MoveAndCollide(motion);

            if (collision != null)
            {
                var collider = collision.GetCollider();

                if (collider is Node node)
                {
                    HandleHit(node);
                }
                else
                {
                    GD.Print("Hit something non-node: ", collider);
                    DestroySelf();
                }

                return;
            }
        }
    }
    private void HandleHit(Node node)
    {
        Node current = node;

        while (current != null)
        {
            if (current is FPSController player)
            {
                player.Hit(damage, ownerId, player.GetMultiplayerAuthority());
                break;
            }

            current = current.GetParent();
        }
        DestroySelf();
    }

   private void DestroySelf()
    {
        if (!GenericCore.Instance.IsServer)
            return;
            
        GenericCore.Instance.Rpc(nameof(GenericCore.DestroyBulletVisualGlobal), bulletId);
        QueueFree();
    }
}