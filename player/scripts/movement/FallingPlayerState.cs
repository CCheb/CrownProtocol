using Godot;
using System;

public partial class FallingPlayerState : PlayerMovementState
{
	// Falling specific movement variables
    [Export] public float acceleration = 0.1f;
    [Export] public float decelaration = 0.25f;
    [Export] public float doubleJumpVelocity = 5.5f;
    private float speed = 6.0f;
    private bool doubleJump = false;

    public override void Init()
    {
        StateName = Globals.MovementStates.Fall;
        MovementProfle = default;
    }
    public override void Enter(State prevState)
    {
        base.Enter(prevState);
        ANIMATION.Pause();
        speed = PLAYER.speed;
    }

    public override void Exit()
    {
        base.Exit();
        doubleJump = false;
    }

    public override void Update(double delta)
    {
        base.Update(delta);

        // Update player movment accordingly
        PLAYER.UpdateGravity(delta);
        // Small change to passed velocity to limit the player movement
        PLAYER.UpdateInput(speed, acceleration, decelaration);
        PLAYER.UpdateVelocity();

        // Enables a jump while falling this state runs when we fall off a ledge
        // for a certain amount of time. Its slightly different from the jump state
        if (Input.IsActionJustPressed("jump") && !doubleJump)
        {
            Vector3 velocity = PLAYER.Velocity;
            velocity.Y = doubleJumpVelocity;
            PLAYER.Velocity = velocity;
            doubleJump = true;
        }

        if (PLAYER.IsOnFloor())
        {
            // By specifying JumpEnd as the animation, the other states are configured
            // to adjust themselves if the current animation is JumpEnd!
            ANIMATION.Play("JumpEnd");
            EmitSignal(SignalName.Transition, "IdlePlayerState");
        }

        

    }
}
