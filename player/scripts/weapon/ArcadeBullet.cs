using Godot;
using System;
using System.ComponentModel;

public partial class ArcadeBullet : CharacterBody3D, IBullet
{
	[Export] private Area3D area;
	private float speed = 50f;
	// Called when the node enters the scene tree for the first time.
	public void Initialize(Transform3D transform)
	{
		GlobalTransform = transform;
		GD.Print($"Bullet Spawned at {GlobalTransform}");
	}
	public override void _Ready()
	{
		if(!GenericCore.Instance.IsServer)
			return;

		area.BodyEntered += OnBodyEntered;

		StartBulletTimeout();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (!GenericCore.Instance.IsServer)
			return;

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
