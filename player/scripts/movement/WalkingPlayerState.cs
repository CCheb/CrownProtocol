using Godot;
using System;
using System.Security.AccessControl;

public partial class WalkingPlayerState : PlayerMovementState
{
	// Walking specific movement variables
    [Export] public float topAnimationSpeed = 1.8f;
    // Variables used for players UpdateInput()
    [Export] public float acceleration = 0.1f;
    [Export] public float decelaration = 0.25f;
    
    private float speed = 6.0f;

    [ExportGroup("Weapon Movement Profile")]
    [Export] public bool IsIdle = false;
    [Export] public float BobSpeed = 5.0f;
    [Export] public float BobH = 2.0f;
    [Export] public float BobV = 8.0f;

    public override void Init()
    {
        StateName = Globals.MovementStates.Walk;
        MovementProfle = new Globals.WeaponMovementProfle
        {
            IsIdle = false,
            BobSpeed = this.BobSpeed,
            HorizontalBobAmount= this.BobH,
            VerticalBobAmount = this.BobV
        };
    }

    // On enter we want to play the walking animation and as long as the players velocity 
    // is > 0.0, we will keep being in this state and the walking animtation will keep looping
    public override async void Enter(State prevState)
    {
        base.Enter(prevState);

        if (PLAYER.myNetId.IsLocal)
        {
            // If the current animation is the JumpEnd animation then wait for it to finish
            // before playing the states animation
            if (ANIMATION.IsPlaying() && ANIMATION.CurrentAnimation == "JumpEnd")
                await ToSignal(ANIMATION, "animation_finished");
            ANIMATION.Play("Walk", -1.0, 1.0f);
            
        }

        PLAYER.speed = speed;
        PLAYER.acceleration = acceleration;
        PLAYER.deceleration = decelaration;

    }

    public override void Exit()
    {
        base.Exit();

        if (PLAYER.myNetId.IsLocal)
            // Make sure to reset the speed scale
            ANIMATION.SpeedScale = 1.0f;
    }

    // Called in state machine's _Process()
    public override void Update(double delta)
    {
        // While in this state we constantly check if the players velocity has reached 0.0
        // if so then we send a signal to change to idle state
        base.Update(delta);

        /*
        // In each update call, we update the player movement
        PLAYER.UpdateGravity(delta);
        PLAYER.UpdateInput(speed, acceleration, decelaration);
        PLAYER.UpdateVelocity(); 
        /*

        // Want to sway weapon but not random idle sway
        //WEAPON.SwayWeapon(delta, false);
        // Weapon bobbing while in this state
        //WEAPON.WeaponBob(delta, bobSpeedWeapon, bobWeaponH, bobWeaponV);

        
        // Pass in dynamic velocity! If we run or crouch the bobbing will also adjust
        SetAnimationSpeed(PLAYER.Velocity.Length());

        /*
        // We check inside of update to make sure we are the only state triggering state switches Sprint state 
        if (Input.IsActionPressed("sprint") && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "SprintingPlayerState");

        // Crouch state
        if(Input.IsActionPressed("crouch") && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "CrouchingPlayerState");

        // Transition over to Jumping Player State
        if (Input.IsActionJustPressed("jump") && PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "JumpingPlayerState");
            
        // Transition over to Fallling Player State. Gravity will eventually
        // pull this down to -3
        if(PLAYER.Velocity.Y < -3.0f && !PLAYER.IsOnFloor())
            EmitSignal(SignalName.Transition, "FallingPlayerState");
        */

        // The state machine its whats subscribed to these signals
        if (PLAYER.GetDerivedVelocity().Length() < 0.1f)
            EmitSignal(SignalName.Transition, "IdlePlayerState");
        
    }

    private void SetAnimationSpeed(float currSpeed)
    {
        if (!PLAYER.myNetId.IsLocal)
            return;
        
        // As player velocity increase, the playback speed increases
        // If speed is in between the mins, then its shifted between the 0.0f, 1.0f range
        var alpha = Mathf.Remap(currSpeed, 0.0f, speed, 0.0f, 1.0f);
        // Linearly interpolate from 0.0f to topAnimationSpeed with defined alpha weight
        ANIMATION.SpeedScale = (float)Mathf.Lerp(0.0, topAnimationSpeed, alpha);
    }
}
