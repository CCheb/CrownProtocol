using Godot;
using System;

public partial class Reticle : CenterContainer
{
	// Array for holding the 4 reticle lines
	[Export] public Line2D[] reticleLines;
	// Need to correlate the player speed with the reticle 
	[Export] public CharacterBody3D playerController;
	// How fast the recticle moves
	[Export] float reticleSpeed = 0.25f;
	// How far out will the reticle move
	[Export] float reticleDistance = 3.0f;
	[Export] public float dotRadius = 2.0f;
	[Export] public Color dotColor = new Color(1.0f, 1.0f, 1.0f);
	[Export] public Color enemyColor = new Color(1.0f, 0.0f, 0.0f);
	private bool isColliding = false;

	public override void _Ready()
	{
		QueueRedraw();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Continuously update the reticle
		AdjustReticleLines();
	}

	public override void _Draw()
	{
		base._Draw();
		// Drawing the center dot of the reticle. Initial drawing of the middle dot
		DrawCircle(new Vector2(0.0f, 0.0f), dotRadius, isColliding ? enemyColor : dotColor);
	}

	private void AdjustReticleLines()
	{
		// Its important that we grab the physical/in-game velocity of the player and not just
		// inputs. This will take into account gravity, sliding, and other movements beyond just the input
		Vector3 vel = playerController.GetRealVelocity();

		// Need it to offset the reticle. Pos helps us anchor the reticle and have a reference point. It will move relative to it
		// Right now since its set to (0,0) it doesnt really matter much since we are adding (0,0) but if it was at another location
		// then we need to take it under consideration.
		Vector2 pos = new Vector2(0.0f, 0.0f);

		// We only care about the magnitude/strength of the velocity when changing the crosshair
		float speed = vel.Length();

		// Finally we lerp the position of each of the sticks in an appropriate direction and ammount
		// We scale the target based on the speed of the movement and the preset reticleDistance
		reticleLines[0].Position = reticleLines[0].Position.Lerp(pos + new Vector2(0, -speed * reticleDistance), reticleSpeed);
		reticleLines[1].Position = reticleLines[1].Position.Lerp(pos + new Vector2(speed * reticleDistance, 0), reticleSpeed);
		reticleLines[2].Position = reticleLines[2].Position.Lerp(pos + new Vector2(0, speed * reticleDistance), reticleSpeed);
		reticleLines[3].Position = reticleLines[3].Position.Lerp(pos + new Vector2(-speed * reticleDistance, 0), reticleSpeed);
	}

	public void EnemyDetection(bool enColl)
    {
		// Set the private bool var accordingly and tell it to redraw the circle
		isColliding = enColl;
		QueueRedraw();
		// Go through each line and update the default colors accordingly
		for(int i = 0; i < 4; i++)
			reticleLines[i].DefaultColor = enColl ? enemyColor : dotColor;
    }
}
