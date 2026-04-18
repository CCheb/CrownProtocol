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
        GD.Load<WeaponResource>("res://player/assets/weapons/smg/smgResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/burstRifle/burstRifleResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/shotgun/shotgunResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/bazooka/bazookaResource.tres"),
        GD.Load<WeaponResource>("res://player/assets/weapons/sniper/sniperResource.tres")
    }; 

    // This variable would be the most important to synchronize
    private int LastWeaponIndex = 0;
    [Export] public int CurrentWeaponIndex = 0;
    private const int MAX_WEAPON_AMMOUNT = 6;
    private WeaponBase CurrentWeapon;
    private Procedural procedural = new();
    private IWeaponAction CurrentPrimaryWeaponAction;
    private IWeaponAction CurrentSecondaryWeaponAction;
    [Export] public CameraController CameraControllerRef;
    [Export] public WeaponRecoil WeaponRecoilRef;
    [Export] public JumpRecoil JumpRecoilRef;
	[Export] private NoiseTexture2D RandSwayNoise;
    [Export] private AmmoCounter ammoCounter;
    [Export] public MuzzleFlash characterMuzzleFlash;

    public PlayerContext context;
    private const int SERVER = 1;

    // Runs before _Ready()
    public void SetContext(PlayerContext ctx)
    {
        context = ctx;
    }

    public override void _Ready()
    {
        base._Ready();

        SetPhysicsProcess(false);
        SetProcess(false);

        LastWeaponIndex = CurrentWeaponIndex;

        context.player.PlayerReady += OnPlayerReady;        
    }

    private void OnPlayerReady()
    {
        GD.Print("WeaponController ready!");
        
        SetPhysicsProcess(true);
        SetProcess(true);

        // All peers load weapon
        LoadWeapon();

        if(!context.player.myNetId.IsLocal)
            return;

        procedural.SetCurrentWeaponMovementProfile(CurrentWeaponMovementProfile);
        procedural.SetRandSwayNoise(RandSwayNoise);
    }

    public override void _Input(InputEvent @event)
    {
        if (!context.player.myNetId.IsLocal)
            return;

        base._Input(@event);
        if (@event is InputEventMouseMotion)
		{
			// Need to cast event over to InputEventMouseMotion, copy that into a local variable and
			// pass the Relative (mouse deltas between frames) over to MouseMovement 
			InputEventMouseMotion MouseEvent = (InputEventMouseMotion)@event;
            procedural.SetMouseMovementDelta(MouseEvent);
		}
        
        // WeaponAction only cares on when the current weapon should shoot.
        if(@event.IsActionPressed("reload"))
            CurrentWeapon?.Reload();

        // When in input is pressed, no matter where it is, godot will broadcast that input to
        // all implemented _Input() functions throughout the project
        for(int i = 1; i <= MAX_WEAPON_AMMOUNT; i++)
        {   
            if(@event.IsActionPressed($"weapon_{i}"))
            {
                RpcId(SERVER, MethodName.TryWeaponSwap, i-1);
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void TryWeaponSwap(int ProposedWeapon)
    {
        // If the proposed weapon is already the same as the CurrentWeaponIndex then dont do anything
        if(ProposedWeapon == CurrentWeaponIndex || ProposedWeapon > Arsenal.Length || Arsenal[ProposedWeapon].Status == Globals.WeaponStatus.Locked)
            return;

        // Server changes this
        CurrentWeaponIndex = ProposedWeapon;
    }
    
    private void LoadWeapon()
    {
        // Could be for everyone
        UpdateCurrentWeapon();
        UpdateCurrentWeaponActions();

        if (context.player.myNetId.IsLocal || GenericCore.Instance.IsServer)
        {
            ParseWeaponResource(in Arsenal[CurrentWeaponIndex]);
            JumpRecoilRef.AddChild(CurrentWeapon, true);
            CameraControllerRef.SetCameraReloadLayer(CurrentWeapon.CameraReloadProxy);  
            GD.Print($"Ammo count: {Arsenal[CurrentWeaponIndex].AmmoCount}"); 
        }
            
    }

    private void UpdateCurrentWeapon()
    {
        // Local Player = build weapon; Non-local Player = swap weapon models
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
        // Local player only
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

        // Every loads weapons when they notice a change
        if(LastWeaponIndex != CurrentWeaponIndex)
        {
            GD.Print("Swaping Weapon!!!");
            LastWeaponIndex = CurrentWeaponIndex;
            SwapWeapon();   // Clients and server would diverge from here

            if(context.player.myNetId.IsLocal)
                ammoCounter.UpdateText(Arsenal[CurrentWeaponIndex].AmmoCount);
        }

        if (!context.player.myNetId.IsLocal)
            return;

        Vector3 WeaponPos = Position;
		Vector3 WeaponRotDeg = RotationDegrees;

        procedural.ApplyProceduralWeaponMovement(ref WeaponPos, ref WeaponRotDeg, delta);

        Position = WeaponPos;
        RotationDegrees = WeaponRotDeg;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!context.player.myNetId.IsLocal)
            return;
        
        // In _Input, the event actions are not polled and are only triggered once everytime the key is pressed 
        if(Input.IsActionJustPressed("primary_action"))
            CurrentPrimaryWeaponAction?.OnActionPressed();
        
        if(Input.IsActionJustReleased("primary_action"))
            CurrentPrimaryWeaponAction?.OnActionReleased();

        if(Input.IsActionJustPressed("secondary_action"))
            CurrentSecondaryWeaponAction?.OnActionPressed();
        
        if(Input.IsActionJustReleased("secondary_action"))
            CurrentSecondaryWeaponAction?.OnActionReleased();


        CurrentPrimaryWeaponAction?.Update(delta);
        CurrentSecondaryWeaponAction?.Update(delta);
    }

    // This will most likely be an rpc done on the server
    private void SwapWeapon()
    {
        CurrentWeapon?.QueueFree();
        // By this time the CurrentWeaponIndex has already moved to the next weapon
        LoadWeapon();
    }

    // Triggered every movement state change
    public void OnMovementStateChange(State NextMovementState)
    {
        if (!context.player.myNetId.IsLocal)
            return;

        CurrentMovementState = NextMovementState.GetStateName();
        CurrentWeaponMovementProfile = NextMovementState.GetWeaponProfile();
        procedural.SetCurrentWeaponMovementProfile(CurrentWeaponMovementProfile);
    }


    public void OnWeaponPickedUp(int pickedWeapon)
    {
        GD.Print("Server sensed the pickup!");

        if (Arsenal[pickedWeapon].Status == Globals.WeaponStatus.Locked)
        {
            CurrentWeaponIndex = pickedWeapon; // Will automatically cause a Swap weapon to occur
            Arsenal[pickedWeapon].Status = Globals.WeaponStatus.Unlocked;
        }
        else
        {
            GD.Print("Weapon already unlocked; picking up some ammo");
            RpcId(context.player.myNetId.OwnerId, MethodName.AddAmmoTo, pickedWeapon);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void AddAmmoTo(int weaponIndex)
    {
        if(!context.player.myNetId.IsLocal)
            return;

        GD.Print("Adding ammo!!");
        Arsenal[weaponIndex].AmmoCount = Arsenal[weaponIndex].AmmoCapacity;
        ammoCounter.AmmoSpent(Arsenal[weaponIndex].AmmoCount);
    }

    public void UpdateCurrentWeaponAmmo()
    {
        Arsenal[CurrentWeaponIndex].AmmoCount--;
        ammoCounter.AmmoSpent(Arsenal[CurrentWeaponIndex].AmmoCount);
    }

    public int CheckCurrentWeaponAmmo()
    {
        return Arsenal[CurrentWeaponIndex].AmmoCount;
    }

    public float GetCurrentWeaponsDamage()
    {
        return Arsenal[CurrentWeaponIndex].damage;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    public void BroadCastShooting(bool condition)
    {
        if(context.player.myNetId.IsLocal || GenericCore.Instance.IsServer)
            return;

        context.movementStateMachine.ToogleStateAnimation(!condition);

        switch(context.movementStateMachine.GetCurrentStateName())
        {
            case Globals.MovementStates.Idle:
                //context.player.characterAnimations.Set("parameters/conditions/idle", false);
                context.player.characterAnimations.Set("parameters/conditions/shooting", condition);
                context.player.characterAnimations.Set("parameters/conditions/runShoot", false);
                context.player.characterAnimations.Set("parameters/conditions/jump_shoot", false);
                break;
            case Globals.MovementStates.Walk:
                context.player.characterAnimations.Set("parameters/conditions/runShoot", condition);
                context.player.characterAnimations.Set("parameters/conditions/shooting", false);
                context.player.characterAnimations.Set("parameters/conditions/jumpShoot", false);
                break;
            case Globals.MovementStates.Jump:
                context.player.characterAnimations.Set("parameters/conditions/jumpShoot", condition);
                context.player.characterAnimations.Set("parameters/conditions/shooting", false);
                context.player.characterAnimations.Set("parameters/conditions/runShoot", false);
                break;
        }

    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RefreshAllAmmo()
    {
        for(int i = 0; i < Arsenal.Count(); i++)
            Arsenal[i].AmmoCount = Arsenal[i].AmmoCapacity;
    }

   
}
