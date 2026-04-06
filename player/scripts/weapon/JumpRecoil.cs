using Godot;
using System;

public partial class JumpRecoil : Node3D
{
	[Signal] public delegate void AddJumpRecoilEventHandler();
	private Vector3 targetPosition = Vector3.Zero;
	private Vector3 currentPosition = Vector3.Zero;
	private Quaternion targetRotation = Quaternion.Identity;
	private Quaternion currentRotation = Quaternion.Identity;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{	
		// Make sure the signal is properly connected!
		AddJumpRecoil += AddRecoil;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		targetRotation = targetRotation.Normalized();
		currentRotation = currentRotation.Normalized();

		// Here we lerp the currentPosition based on the targetPosition which receives an addition when the signal is fired
		targetPosition = targetPosition.Lerp(Vector3.Zero, 5.0f*(float)delta);
		currentPosition = currentPosition.Lerp(targetPosition, 5.0f*(float)delta);

		// Slerp is for spherical interpolation. In this case we want the weapon to tilt slightly downwards when it hits the floor
		// We slerp the currentRotation based on the targetRotation. We use Quaternions instead of euler angles for better smoothness
		targetRotation = targetRotation.Slerp(Quaternion.Identity, 6.0f*(float)delta);
		currentRotation = currentRotation.Slerp(targetRotation, 5.0f*(float)delta);

		// Continously apply the currentPosition and Rotation changes. If the signal doesnt get fired then this changes nothing
		Position = currentPosition;
		Quaternion = currentRotation;

		
	}

	private void AddRecoil()
	{	
		// Add a small amount downwards when the player impacts the floor
		targetPosition += new Vector3(0.0f, -0.05f, 0.0f);
		
		// Gererate a slight tilt downward when the player impacts the floor
		Quaternion recoil = Quaternion.FromEuler(
    		new Vector3(Mathf.DegToRad(-15.0f), 0.0f, 0.0f)
		);
		targetRotation = (recoil * targetRotation).Normalized();

	}
}
