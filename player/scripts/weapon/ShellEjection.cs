using Godot;
using System;

public partial class ShellEjection : RigidBody3D
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		// Give the shell casing a random angular rotation as soon as it spawns
        AngularVelocity = new Vector3(
            GD.RandRange(-15, 15),
            GD.RandRange(-15, 15),
            GD.RandRange(-15, 15)
        );

		// delete after 10 seconds
    	GetTree().CreateTimer(10).Timeout += QueueFree;
    }
	public void Eject(Vector3 direction, float force)
    {
		// Apply impulse only once so that the shell casing flies out of the gun
        LinearVelocity = Vector3.Zero;
        ApplyImpulse(direction * force);

    }
}
