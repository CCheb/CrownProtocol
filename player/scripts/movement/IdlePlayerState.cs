using Godot;
using System;

public partial class IdlePlayerState : PlayerMovementState
{	
	// Idle specific movement variables. Used in player's UpdateInput() 
    [Export] public float acceleration = 0.1f;
    [Export] public float decelaration = 0.25f;
    private float speed;

    public override void Init()
    {
        StateName = Globals.MovementStates.Idle;
        MovementProfle = new Globals.WeaponMovementProfle
        {
            IsIdle = true,
            BobSpeed = 0.0f,
            HorizontalBobAmount = 0.0f,
            VerticalBobAmount = 0.0f
        };
    }

    // When entering idle, we pause any aminations which will most likely be walking
    public override async void Enter(State prevState)
    {
        // Calling the Enter of the State class. No need to call base.Enter since it dont do anything
        base.Enter(prevState);
        // If the current animation is the JumpEnd animation then wait for it to finish
        // before playing the states animation
        if (ANIMATION.IsPlaying() && ANIMATION.CurrentAnimation == "JumpEnd")
            await ToSignal(ANIMATION, "animation_finished");
        
        ANIMATION.Pause();
        speed = PLAYER.speed;
        //GD.Print("Entered idle state");
    }

    public override void Exit()
    {
        base.Exit();
        // Make sure to reset the speed scale
        ANIMATION.SpeedScale = 1.0f;
    }
    public override void Update(double delta)
    {
        // Emmit transition signal to walking state if the player's velocity is > 0.0f
        base.Update(delta);

        // Update player movment accordingly. As if you had them together
        PLAYER.UpdateGravity(delta);
        PLAYER.UpdateInput(speed, acceleration, decelaration);
        PLAYER.UpdateVelocity();

        // WEAPON.SwayWeapon(delta, true);

        if(Input.IsActionPressed("crouch") && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "CrouchingPlayerState");

        // Notice how this only passes when the player is on the floor. Its important
        // that we check the length of the velocity and not the raw form since it might
        // be negative if we say move forward for example
        if (PLAYER.Velocity.Length() > 0.0f && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "WalkingPlayerState");

        // Transition over to Jumping Player State
        if (Input.IsActionJustPressed("jump") && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "JumpingPlayerState");

        // Transition over to Fallling Player State
        if (PLAYER.Velocity.Y < -3.0f && !PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "FallingPlayerState");
    }
}
