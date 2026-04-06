using Godot;
using System;

public partial class CrouchingPlayerState : PlayerMovementState
{
	// Variables for UpdateInput() under player
    [Export] public float acceleration = 0.1f;
    [Export] public float decelaration = 0.25f;
    // special export where we can specify a range in the editor
    [Export(PropertyHint.Range, "1, 6, 0.1")] public float crouchSpeed { get; set; } = 4.0f;
    [Export] public float crouchSlowDown = 3.0f;
    private float speed = 0.0f;
    private bool RELEASED = false;
    private bool IsFirstTimeIdle = true;
    
    [ExportGroup("Weapon Movement Profile")]
    [Export] public float BobSpeed = 5.0f;
    [Export] public float BobH = 2.0f;
    [Export] public float BobV = 8.0f;
    private Globals.WeaponMovementProfle CrouchIdleMovementProfile;
    private Globals.WeaponMovementProfle CrouchMovementProfile;

    public override void Init()
    {
        StateName = Globals.MovementStates.Crouch;
        CrouchMovementProfile = new Globals.WeaponMovementProfle
        {
            IsIdle = false,
            BobSpeed = this.BobSpeed,
            HorizontalBobAmount = BobH,
            VerticalBobAmount = BobV
        };

        CrouchIdleMovementProfile = new Globals.WeaponMovementProfle
        {
            IsIdle = true,
            BobSpeed = 0.0f,
            HorizontalBobAmount = 0.0f,
            VerticalBobAmount = 0.0f

        };

        // Default MovementProfile variable to Idle Movement Profile
        MovementProfle = CrouchIdleMovementProfile;
    }

    // Play the crouching animation when entering the state
    public override void Enter(State prevState)
    {
        base.Enter(prevState);

        ANIMATION.SpeedScale = 1.0f;
        if (prevState.Name != "SlidingPlayerState")
            ANIMATION.Play("Crouch", -1, crouchSpeed);
        else if (prevState.Name == "SlidingPlayerState")
        {
            ANIMATION.CurrentAnimation = "Crouch";
            // Sets the crouching animation at the end of the animation
            ANIMATION.Seek(1.0, true);
        }

        speed = PLAYER.speed - crouchSlowDown;
    }

    public override void Exit()
    {
        base.Exit();
        RELEASED = false;
    }

    public override void Update(double delta)
    {
        base.Update(delta);

        // In each update call, we update the player movement
        PLAYER.UpdateGravity(delta);
        PLAYER.UpdateInput(speed, acceleration, decelaration);
        PLAYER.UpdateVelocity();

        if (PLAYER.Velocity != Vector3.Zero && !IsFirstTimeIdle)
        {
           // WEAPON.SwayWeapon(delta, false);
           // WEAPON.WeaponBob(delta, bobSpeedWeapon, bobWeaponH, bobWeaponV);
           MovementProfle = CrouchMovementProfile;
           WEAPON_CONTROLLER.EmitSignal(WeaponController.SignalName.MovementChanged, this);
           IsFirstTimeIdle = true;
        }
        else if (PLAYER.Velocity == Vector3.Zero && IsFirstTimeIdle)
        {
            MovementProfle = CrouchIdleMovementProfile;
            WEAPON_CONTROLLER.EmitSignal(WeaponController.SignalName.MovementChanged, this);
            IsFirstTimeIdle = false;
        }

        // Type of crouch: holding
        if (Input.IsActionJustReleased("crouch"))
            Uncrouch();
        // When we are crouching under something and let go of the croucing button
        else if (Input.IsActionPressed("crouch") == false && RELEASED == false)
        {
            RELEASED = true;
            Uncrouch();
        }

    }

    private async void Uncrouch()
    {
        // If we uncrouch and there is nothing above
        if (PLAYER.crouchShapeCast.IsColliding() == false && Input.IsActionPressed("crouch") == false)
        {
            // Play crouching animation in reverse
            ANIMATION.Play("Crouch", -1.0f, -crouchSpeed * 1.5f, true);
            // Wait first for the animation to finish before calling the idle state
            if (ANIMATION.IsPlaying())
                await ToSignal(ANIMATION, "animation_finished");
            EmitSignal(SignalName.Transition, "IdlePlayerState");
        }
        // else there we uncrouched but there is something above us
        else if (PLAYER.crouchShapeCast.IsColliding() == true)
        {
            // Pause for 0.1 seconds and check again (slow update)
            await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
            Uncrouch();
        }
    }
}
