using Godot;
using System;

public partial class Hitscan : WeaponBase
{
    [Export] private PackedScene ImpactEffect;
    [Export] private PackedScene WeaponDecal;
    [Export] private PackedScene ShellCasingScene;
    [Export] private Marker3D ShellEjectionMarker;
    
    // Its vital that we initialize the corresponding WeaponData and Controller variables
    // before we start passing out information from WeaponData
    public override void Initiallize(WeaponResource WeaponData, WeaponController weaponController)
    {
        this.WeaponData = WeaponData;
        this.weaponController = weaponController;
    }

    public override  void _Ready()
    {
        base._Ready();
        
        // No need to initialize Position, Rotation, and Scale here since the WeaponController is already doing that for us
        // We do however need to initialize more weapon specific things like nodes
        SetWeaponNodes();
        FireAnimationSpeed = CalculateFireAnimationSpeed();
        TryPlayingDrawAnimation();
    }

    private void SetWeaponNodes()
    {
        CameraReloadProxy = GetNode<Node3D>("./CameraReloadProxy");
        MuzzleFlashRef = GetNode<MuzzleFlash>("./MuzzleFlash");
        //GunSoundEmpty = GetNode<AudioStreamPlayer3D>("GunSoundEmpty");
        GunSound = GetNode<AudioStreamPlayer3D>("GunSound");
        WeaponAnimPlayer = GetNode<AnimationPlayer>("./Meshes/AnimationPlayer");
    }

    // Function gets called at very specific moments during the firing animation
    public void EjectShell()
    {
        ShellEjection Shell = ShellCasingScene.Instantiate<ShellEjection>();
        GetTree().CurrentScene.AddChild(Shell);
        Shell.GlobalTransform = ShellEjectionMarker.GlobalTransform;
        Vector3 EjectDir = ShellEjectionMarker.GlobalTransform.Basis.X.Normalized();
        Shell.Eject(EjectDir, 0.3f);
    }

    public override async void Fire()
    {
        IsFiring = true;
        
        // Find out if the ray intersected with a body. It will return nothing if not
        Godot.Collections.Dictionary collisionResult = CalculateRay();
        if(collisionResult.Count != 0)
        {
            PlayFireSequence();
            // This gets ignored by certain Actions like FullAuto
            await ToSignal(WeaponAnimPlayer, "animation_finished");
        }

        IsFiring = false;
    }

    private Godot.Collections.Dictionary CalculateRay(float length = 1000.0f)
    {
		Camera3D camera = Globals.player.WorldCameraController.Camera;
		// Grab the worlds 3D physics state/sandbox. This state is where all of the physics occurs and its handled by the physics server
		var spaceState = camera.GetWorld3D().DirectSpaceState;
		
		Vector2 screenCenter = (Vector2)GetViewport().Get("size") / 2;
		
		Vector3 originPoint = camera.ProjectRayOrigin(screenCenter);
		Vector3 endPoint = originPoint + camera.ProjectRayNormal(screenCenter) * length;

		// Create the ray which will return back a dictionary with metadata on any
		// physics collisions. Make sure to enable collision with bodies or areas
		var queryCollisions = PhysicsRayQueryParameters3D.Create(originPoint, endPoint);
        queryCollisions.CollideWithBodies = true;
		queryCollisions.CollideWithAreas = true;
		queryCollisions.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2); // Detect layers 1, 2, and 3

		var collisionResult = spaceState.IntersectRay(queryCollisions);
        return collisionResult;
    }

    private void PlayFireSequence()
    {
        SignalNodes();
        WeaponAnimPlayer.Play(WeaponData.Fire.AnimationName, WeaponData.Fire.BlendAmount,FireAnimationSpeed);
        UpdateAmmo();
        WeaponAnimPlayer.Seek(0.02f, true); // Nudge animation forward to the "kick" pose
        GunSound.Play();
    }

    private void SignalNodes()
    {
        weaponController.CameraControllerRef.RequestCameraRecoil();
        weaponController.WeaponRecoilRef.RequestWeaponRecoil();
        MuzzleFlashRef.RequestMuzzleFlash(WeaponData.FireRate);
    }

    private void UpdateAmmo()
    {
        return;
    }

    // Could make this abstract so that all guns must implement reload
    public override async void Reload()
    {
        // To prevent spam reloads
        if(IsReloading || IsFiring) 
            return;

        // Lock the weapon from firing then play and wait for the recoil animation before unlocking the weapon
        IsReloading = true;
        WeaponAnimPlayer.Play(WeaponData.Reload.AnimationName, WeaponData.Reload.BlendAmount, WeaponData.Reload.AnimationSpeed);
        await ToSignal(WeaponAnimPlayer, "animation_finished");
        IsReloading = false;

    }
}
