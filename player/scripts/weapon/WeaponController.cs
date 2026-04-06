using Godot;
using System;
using System.Linq;

public partial class WeaponController : Node3D
{
    // Will let the WeaponController know if the the current player movement state has changed
    [Signal] public delegate void MovementChangedEventHandler(State NewMovementState);
    private Globals.MovementStates CurrentMovementState = Globals.MovementStates.Idle;
    // We initiallize CurrentWeaponMovementProfile to the Idle profile by default since the signal wont be called automatically
    private Globals.WeaponMovementProfle CurrentWeaponMovementProfile = new Globals.WeaponMovementProfle
    {
        IsIdle = true,
        BobSpeed = 0.0f,
        HorizontalBobAmount= 0.0f,
        VerticalBobAmount = 0.0f
    }; 

    // Everything about the weapon starts here
    private WeaponResource[] Arsenal =
    {
        GD.Load<WeaponResource>("res://player/assets/weapons/DrewPistol/drewPistol.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/rifle/Rigged_WeaponResource.tres"),
    }; 

    // This variable would be the most important to synchronize
    private int CurrentWeaponIndex = 0;
    private const int MAX_WEAPON_AMMOUNT = 2;
    private WeaponBase CurrentWeapon;
    private Procedural procedural = new();
    private IWeaponAction CurrentPrimaryWeaponAction;
    private IWeaponAction CurrentSecondaryWeaponAction;
    [Export] public CameraController CameraControllerRef;
    [Export] public WeaponRecoil WeaponRecoilRef;
    [Export] public JumpRecoil JumpRecoilRef;
	[Export] private NoiseTexture2D RandSwayNoise;


    public override void _Ready()
    {
        base._Ready();
        MovementChanged += OnMovementStateChange;
        LoadWeapon();
        procedural.SetCurrentWeaponMovementProfile(CurrentWeaponMovementProfile);
        procedural.SetRandSwayNoise(RandSwayNoise);
    }

    public override void _Input(InputEvent @event)
    {
        base._Input(@event);
        if (@event is InputEventMouseMotion)
		{
			// Need to cast event over to InputEventMouseMotion, copy that into a local variable and
			// pass the Relative (mouse deltas between frames) over to MouseMovement 
			InputEventMouseMotion MouseEvent = (InputEventMouseMotion)@event;
			//MouseMovement = MouseEvent.Relative;
            procedural.SetMouseMovementDelta(MouseEvent);
		}

        // In _Input, the event actions are not polled and are only triggered once everytime the key is pressed 
        if(@event.IsActionPressed("primary_action"))
            CurrentPrimaryWeaponAction?.OnActionPressed();

        if(@event.IsActionReleased("primary_action"))
            CurrentPrimaryWeaponAction?.OnActionReleased();
    
        if(@event.IsActionPressed("secondary_action"))
            CurrentSecondaryWeaponAction?.OnActionPressed();
        
        if(@event.IsActionReleased("secondary_action"))
            CurrentSecondaryWeaponAction?.OnActionReleased();
        
        // WeaponAction only cares on when the current weapon should shoot.
        if(@event.IsActionPressed("reload"))
            CurrentWeapon?.Reload();

        // When in input is pressed, no matter where it is, godot will broadcast that input to
        // all implemented _Input() functions throughout the project
        for(int i = 1; i <= MAX_WEAPON_AMMOUNT; i++)
        {   
            if(@event.IsActionPressed($"weapon_{i}"))
                TryWeaponSwap(i - 1);
        }
    }

    private void TryWeaponSwap(int ProposedWeapon)
    {
        // If the proposed weapon is already the same as the CurrentWeaponIndex then dont do anything
        if(ProposedWeapon == CurrentWeaponIndex || ProposedWeapon > Arsenal.Length)
            return;
    
        CurrentWeaponIndex = ProposedWeapon;

        // Send RPC here?
        SwapWeapon();
    }
    
    // This will most likely be an rpc done on the server
    private void SwapWeapon()
    {
        CurrentWeapon?.QueueFree();
        // By this time the CurrentWeaponIndex has already moved to the next weapon
        LoadWeapon();
    }

    private void LoadWeapon()
    {
        UpdateCurrentWeapon();
        UpdateCurrentWeaponActions();
        ParseWeaponResource(in Arsenal[CurrentWeaponIndex]);
        JumpRecoilRef.AddChild(CurrentWeapon);
        CameraControllerRef.SetCameraReloadLayer(CurrentWeapon.CameraReloadProxy);
    }

    private void UpdateCurrentWeapon()
    {
        CurrentWeapon = WeaponFactory.Create(Arsenal[CurrentWeaponIndex], this);
        if(CurrentWeapon == null)
        {
            GD.PrintErr("CurrentWeapon is null (Invalid Weapon Type)");
            return;
        }
    }

    private void UpdateCurrentWeaponActions()
    {
        // Ask FireModeFactory to Create the appropriate firemode object based on what the WeaponResource specified
        if(Arsenal[CurrentWeaponIndex].PrimaryWeaponAction == Globals.WeaponActions.NoAction)
            return;
    
        CurrentPrimaryWeaponAction = FireModeFactory.CreateNewWeaponAction(this, Arsenal[CurrentWeaponIndex], CurrentWeapon, Arsenal[CurrentWeaponIndex].PrimaryWeaponAction);
        if(CurrentPrimaryWeaponAction == null)
        {
            GD.PrintErr("PrimaryFireMode is null (Invalid Fire Mode Type)");
        }

        if(Arsenal[CurrentWeaponIndex].SecondaryWeaponAction == Globals.WeaponActions.NoAction)
            return;

        CurrentSecondaryWeaponAction = FireModeFactory.CreateNewWeaponAction(this, Arsenal[CurrentWeaponIndex], CurrentWeapon, Arsenal[CurrentWeaponIndex].SecondaryWeaponAction);
        if(CurrentSecondaryWeaponAction == null )
        {
            GD.PrintErr("SecondaryFireMode is null (Invalid Fire Mode Type)");
        } 
    }

    private void ParseWeaponResource(in WeaponResource weaponResource)
    {
        Position = weaponResource.ViewportPosition;
        RotationDegrees = weaponResource.ViewportRotation;
        Scale = weaponResource.ViewportScale;

        procedural.ParseWeaponResource(weaponResource);

        CameraControllerRef.SetCameraRecoilProperties(
            weaponResource.CameraRecoilAmount,
            weaponResource.CameraSnapAmount,
            weaponResource.CameraRecoverySpeed
        );

        WeaponRecoilRef.SetWeaponRecoilProperties(
            weaponResource.WeaponRecoilAmount,
            weaponResource.WeaponSnapAmount,
            weaponResource.WeaponRecoverySpeed
        );
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        Vector3 WeaponPos = Position;
		Vector3 WeaponRotDeg = RotationDegrees;

        procedural.ApplyProceduralWeaponMovement(ref WeaponPos, ref WeaponRotDeg, delta);

        Position = WeaponPos;
        RotationDegrees = WeaponRotDeg;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        CurrentPrimaryWeaponAction?.Update(delta);
        CurrentSecondaryWeaponAction?.Update(delta);
    }

    // Triggered every movement state change
    private void OnMovementStateChange(State NextMovementState)
    {
        CurrentMovementState = NextMovementState.GetStateName();
        CurrentWeaponMovementProfile = NextMovementState.GetWeaponProfile();
        procedural.SetCurrentWeaponMovementProfile(CurrentWeaponMovementProfile);
    }
}
