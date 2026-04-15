using Godot;
using System;
using System.IO;

public abstract partial class ItemPickup : Node3D
{	
	[Export] protected Area3D area;
	[Export] protected AudioStreamPlayer3D audio;
	[Export] protected float itemHeightFromGround;
	[Export] protected float pickupTimeout = 20.0f;
	protected float originBase;
	private float time = 0.0f;
	private float frequency = 3.0f;
	private float amplitude = 0.3f;
	private float angle = 3.0f;

    public override void _Ready()
    {
        base._Ready();
		originBase = GlobalPosition.Y;
    }

	public override void _Process(double delta)
	{
		// Everyone calculates the movement of the pickup independently
		time += (float)delta;

		Vector3 position = Position;
		position.Y = originBase + itemHeightFromGround + Mathf.Sin(time * frequency) * amplitude;
		Position = position;

		Rotate(Transform.Basis.Y.Normalized(), Mathf.DegToRad(angle));
	}

	protected void ToggleItem(bool condition)
	{	
		// All three are synchronized
		Visible = condition;
		area.SetDeferred("monitoring", condition);
		area.SetDeferred("monitorable", condition);
	}

	public abstract void OnBodyEntered(Node3D body);
}
