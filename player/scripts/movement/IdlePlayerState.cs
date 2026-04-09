using Godot;
using System;

public partial class IdlePlayerState : PlayerMovementState
{	
	// Idle specific movement variables. Used in player's UpdateInput() 
    [Export] public float acceleration = 0.1f;
    [Export] public float decelaration = 0.25f;
    private float speed = 6.0f;

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
        base.Enter(prevState);

        if (PLAYER.myNetId.IsLocal)
        {
            // If the current animation is the JumpEnd animation then wait for it to finish
            // before playing the states animation
            if (ANIMATION.IsPlaying() && ANIMATION.CurrentAnimation == "JumpEnd")
                await ToSignal(ANIMATION, "animation_finished");
        
            ANIMATION.Pause();
        }

        PLAYER.speed = speed;
        PLAYER.acceleration = acceleration;
        PLAYER.deceleration = decelaration;

        GD.Print("Entered idle state");
    }

    public override void Exit()
    {
        base.Exit();

        if (PLAYER.myNetId.IsLocal)
            // Make sure to reset the speed scale
            ANIMATION.SpeedScale = 1.0f;

        GD.Print("Exited idle state");
    }
    public override void PhysicsUpdate(double delta)
    {
        base.PhysicsUpdate(delta);
        
        // Server would determine when the player can switch states and broadcast that to all
        // other clients
        if (!GenericCore.Instance.IsServer)
            return;

        if (PLAYER.Velocity.Length() > 0.1f && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "WalkingPlayerState");
    }
}
