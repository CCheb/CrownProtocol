using Godot;
using System;

public partial class EnemyDetection : RayCast3D
{
	// Keep reference to the reticle node so that we can make it call functions
	[Export] private Reticle reticle;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
    {
		// Ray is set to only consider enemy collisions (collision mask = 4)
        if(IsColliding())
            reticle.EnemyDetection(true);
        else
			reticle.EnemyDetection(false);
    }
}
