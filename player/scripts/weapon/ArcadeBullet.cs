using Godot;
using System;
using System.ComponentModel;

public partial class ArcadeBullet : CharacterBody3D, IBullet
{
	[Export] private Area3D area;
	[Export] private float damage = 5.0f;
	private long ownerId = -1;
	private long receiverId = -1;
	[Export] public float speed = 50f;
	private Vector3 direction;
	// Called when the node enters the scene tree for the first time.
	public void Initialize(Transform3D transform, long ownerId)
	{
		GlobalTransform = transform;
		Velocity = -Transform.Basis.Z * speed;
		this.ownerId = ownerId;
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

		
		MoveAndSlide();
	}

	public void OnBodyEntered(Node3D body)
	{
		if(body is FPSController player)
			player.Hit(damage, ownerId, player.myNetId.OwnerId);
		if(body is Enemy enemy)
			enemy.TakeDamage(damage, ownerId);
		QueueFree();
	}

	private async void StartBulletTimeout()
	{
		await ToSignal(GetTree().CreateTimer(10), "timeout");

		QueueFree();
	}
}
