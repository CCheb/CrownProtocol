using Godot;
using System;

public partial class EnemyProjectileVisual : Node3D
{
    public Vector3 direction;
    public float speed = 20f;
    public float lifetime = 5f;
    private float lifeTimer = 0f;
    public int bulletId;

    public override void _PhysicsProcess(double delta)
    {
        // Move forward
        Translate(direction * speed * (float)delta);

        // Lifetime cleanup
        lifeTimer += (float)delta;
        if (lifeTimer >= lifetime)
        {
            QueueFree();
        }
    }
}