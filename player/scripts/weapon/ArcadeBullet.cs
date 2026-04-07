using Godot;
using System;
using System.ComponentModel;

public partial class ArcadeBullet : CharacterBody3D, IBullet
{
	[Export] private Area3D area;
	private float speed;
	// Called when the node enters the scene tree for the first time.
	public void Initialize(Transform3D transform, float speed)
	{
		GlobalTransform = transform;
		this.speed = speed;
	}
	public override void _Ready()
	{
		area.BodyEntered += OnBodyEntered;

		StartBulletTimeout();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Velocity = -Transform.Basis.Z * speed;
		MoveAndSlide();
	}

	public void OnBodyEntered(Node3D body)
	{
		QueueFree();
	}

	private async void StartBulletTimeout()
	{
		await ToSignal(GetTree().CreateTimer(10), "timeout");

		QueueFree();
	}
}
