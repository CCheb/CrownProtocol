using Godot;
using System;

public partial class WeaponRecoil : Node3D
{
	// Custom weapon signal 
	[Signal] public delegate void WeaponFiredSignalEventHandler();
	// Holds the recoil amounts
	[Export] public Vector3 recoilAmount = Vector3.Zero;
	// How sharply the weapon follows the recoil kickback
	[Export] public float snapAmount = 0.0f;
	// How fast does the recoil kickback recover
	[Export] public float speed = 0.0f;

	// Current weapon rotation follows targetPosition with snapAmount
	private Vector3 currentPosition;
	// Actual weapon recoil 
	private Vector3 targetPosition;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		WeaponFiredSignal += AddRecoil;
    }

	public void RequestWeaponRecoil()
	{
		EmitSignalWeaponFiredSignal();
	}

	// IDEA: Make an abstract Recoil class
	public void SetWeaponRecoilProperties(Vector3 recoilAmount, float snapAmount, float speed)
	{
		this.recoilAmount = recoilAmount;
		this.snapAmount = snapAmount;
		this.speed = speed;
	}
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Always going back to zero/base
		targetPosition = targetPosition.Lerp(Vector3.Zero, speed * (float)delta);
		currentPosition = currentPosition.Lerp(targetPosition, snapAmount * (float)delta);
		Position = currentPosition;
	}
	
	private void AddRecoil()
    {
		targetPosition += new Vector3((float)GD.RandRange(recoilAmount.X, recoilAmount.X), (float)GD.RandRange(recoilAmount.Y,
			recoilAmount.Y * 2.0f), (float)recoilAmount.Z * 2.0f);
    }
}
