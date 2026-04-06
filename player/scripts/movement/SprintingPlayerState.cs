using Godot;
using System;

public partial class SprintingPlayerState : PlayerMovementState
{
	// Sprinting specific movement variables
    [Export] public float topAnimationSpeed = 2.2f;
    // Variables for UpdateInput() under player
    [Export] public float acceleration = 0.1f;
    [Export] public float decelaration = 0.25f;
    [Export] public float speedAddOn = 1.0f;
    private float speed = 0.0f;

    [ExportGroup("Weapon Movement Profile")]
    [Export] public bool IsIdle = false;
    [Export] public float BobSpeed = 5.0f;
    [Export] public float BobH = 4.0f;
    [Export] public float BobV = 8.0f;

    public override void Init()
    {
        StateName = Globals.MovementStates.Sprint;
        MovementProfle = new Globals.WeaponMovementProfle
        {
            IsIdle = this.IsIdle,
            BobSpeed = this.BobSpeed,
            HorizontalBobAmount = this.BobH,
            VerticalBobAmount = this.BobV
        };
    }
    // On enter we play the sprinting animation
    public override async void Enter(State prevState)
    {
        base.Enter(prevState);
        // If the current animation is the JumpEnd animation then wait for it to finish
        // before playing the states animation
        if (ANIMATION.IsPlaying() && ANIMATION.CurrentAnimation == "JumpEnd")
            await ToSignal(ANIMATION, "animation_finished");
        ANIMATION.Play("Sprint", 0.5f, 1.0f);
        speed = PLAYER.speed + speedAddOn;
    }

     public override void Exit()
    {
        base.Exit();
        // Make sure to reset the speed scale
        ANIMATION.SpeedScale = 1.0f;
    }

    public override void Update(double delta)
    {
        base.Update(delta);

        // While running make sure to update player movement accordingly
        PLAYER.UpdateGravity(delta);
        PLAYER.UpdateInput(speed, acceleration, decelaration);
        PLAYER.UpdateVelocity();

        // By setting speed to speedSprinting, player will be able to reach
        // that maximum sprint speed over time and while its doing that we also 
        // scale the animation accordingly to reflect the speed change
        SetAnimationSpeed(PLAYER.Velocity.Length());

        // If we release the LShift then go back to the walking state
        // We check the input here so as to not conflict with another state
        // Walking and sprinting might be looking for the Lshift event at the same time
        if (Input.IsActionJustReleased("sprint"))
            EmitSignal(SignalName.Transition, "WalkingPlayerState");

        // Slide when pressing the crouch button and only when at max sprinting speed
        if (Input.IsActionJustPressed("crouch") && PLAYER.Velocity.Length() > 6)
            EmitSignal(SignalName.Transition, "SlidingPlayerState");

        if (Input.IsActionJustPressed("jump") && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "JumpingPlayerState");
        
        // Transition over to Fallling Player State
        if (PLAYER.Velocity.Y < -3.0f && !PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "FallingPlayerState");
        
    }

    private void SetAnimationSpeed(float currSpeed)
    {
        // As player velocity increase, the playback speed increases
        // If speed is in between the mins, then its shifted between the 0.0f, 1.0f range
        // Here we are using the speed which is higher than default sprint
        var alpha = Mathf.Remap(currSpeed, 0.0f, speed, 0.0f, 1.0f);
        // Linearly interpolate from 0.0f to topAnimationSpeed with defined alpha weight
        ANIMATION.SpeedScale = (float)Mathf.Lerp(0.0, topAnimationSpeed, alpha);
    }
}
