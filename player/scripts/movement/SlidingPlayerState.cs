using Godot;
using System;

public partial class SlidingPlayerState : PlayerMovementState
{
	// Sliding specific movement variables. Used in player's UpdateInput()
   
    [Export] public float acceleration = 0.1f;
    [Export] public float decelaration = 0.25f;
    // How much to tilt the camera by
    [Export] public float tiltAmount = 0.09f;
    // special export where we can specify a range in the editor
    [Export(PropertyHint.Range, "1, 6, 0.1")] public float slideAnimSpeed { get; set; } = 4.0f;
    [Export] public float speedAddOn = 1.0f;
    private float speed = 0.0f;

    public override void Init()
    {
        StateName = Globals.MovementStates.Slide;
        MovementProfle = default;
    }
    public override void Enter(State prevState)
    {
        base.Enter(prevState);
        // Pass the players current y rotation which will determine the tilt
        SetTilt(PLAYER._currentRotation);
        SetCameraFov();
        // Alter the speed based on the players velocity. If we are running fast, say at 8.0f
        // then when we slide the speeds first key frame value will be set to 8.0f and interpolate down after that
        // In other words: dynamic sliding
        ANIMATION.GetAnimation("Slide").TrackSetKeyValue(5, 0, PLAYER.Velocity.Length());
        // Ensure animation plays at normal speed
        ANIMATION.SpeedScale = 1.0f;
        // Finally play the animation. once it reaches the end, it will run the finish()
        ANIMATION.Play("Slide", -1.0, slideAnimSpeed);
        speed = PLAYER.speed + speedAddOn;

    }

    public override void Update(double delta)
    {
        base.Update(delta);
        PLAYER.UpdateGravity(delta);
        // Notice how we dont run UpdateInput. This is because we want to lock out the player when sliding
        PLAYER.UpdateVelocity();

    }

    private void SetTilt(float playerRotation)
    {
        // Tilt the camera depending on the players direction. Tilt left if looking left
        // tilt right if looking right. This is determined in the clamp
        Vector3 tilt = Vector3.Zero;
        tilt.Z = (float)Mathf.Clamp(tiltAmount * playerRotation, -0.1, 0.1);
        // if the players rotation is 0 then it will produce a 0 z tilt, thus we specify a fallback
        if (tilt.Z == 0.0)
            tilt.Z = 0.05f;
        // Set the camera's z tilt on key frames 1 and 2 under the rotation track. The fourth keyframe is
        // what resets the tilt back to normal via interpolation
        ANIMATION.GetAnimation("Slide").TrackSetKeyValue(9, 1, tilt);
        ANIMATION.GetAnimation("Slide").TrackSetKeyValue(9, 2, tilt);
    }

    private void SetCameraFov()
    {
        // Setting the camera fov to be in accordance with the current camera fov which is subject to change
        ANIMATION.GetAnimation("Slide").TrackSetKeyValue(7, 0, CAMERA_CONTROLLER.Camera.Fov);
        ANIMATION.GetAnimation("Slide").TrackSetKeyValue(7, 1, CAMERA_CONTROLLER.Camera.Fov+10.0f);
        ANIMATION.GetAnimation("Slide").TrackSetKeyValue(7, 2, CAMERA_CONTROLLER.Camera.Fov);
    }
    
    // Called when the animation finishes. its a special function called in the animation itself
    public void finish()
    {
        // When finished sliding transition over to the crouching state
        // Once in the crouching state, the player will linearly slow down
        // since Input will be reactivated
        EmitSignal(SignalName.Transition, "CrouchingPlayerState");
    }
}
