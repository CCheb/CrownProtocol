using Godot;
using System;

public partial class CameraJumpingLayer : CameraLayer
{
    [Signal] public delegate void AddJumpRecoilEventHandler();

    private Quaternion targetRotation = Quaternion.Identity;
    private Quaternion currentRotation = Quaternion.Identity;
    // If the player does not jump for a long time then RotationOffset will simply pass the Identity matrix which is nothing
    public override Vector3 RotationOffset => currentRotation.GetEuler();

    public override void _Ready()
	{	
		// Make sure the signal is properly connected!
		AddJumpRecoil += AddRecoil;
	}

    public override void _Process(double delta)
    {
        base._Process(delta);

        targetRotation = targetRotation.Normalized();
        currentRotation = currentRotation.Normalized();

        // Slerp is for spherical interpolation. In this case we want the weapon to tilt slightly downwards when it hits the floor
		// We slerp the currentRotation based on the targetRotation. We use Quaternions instead of euler angles for better smoothness
		targetRotation = targetRotation.Slerp(Quaternion.Identity, 6.0f*(float)delta);
		currentRotation = currentRotation.Slerp(targetRotation, 5.0f*(float)delta);

        // Now instead of applying the currentRotation directly its gets automatically tracked by RotationOffset
    }

    private void AddRecoil()
	{	
		
		// Gererate a slight tilt downward when the player impacts the floor
		Quaternion recoil = Quaternion.FromEuler(
    		new Vector3(0.0f, 0.0f, Mathf.DegToRad(-25.0f))
		);
		targetRotation = (recoil * targetRotation).Normalized();

	}
}
