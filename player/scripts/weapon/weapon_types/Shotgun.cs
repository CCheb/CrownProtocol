using Godot;
using System;

public partial class Shotgun : WeaponBase
{
    [Export] private PackedScene ImpactEffect;
    [Export] private PackedScene WeaponDecal;
    [Export] private PackedScene ShellCasingScene;
    [Export] private PackedScene TestDecal;
    [Export] private Marker3D ShellEjectionMarker;
    // Its vital that we initialize the corresponding WeaponData and Controller variables
    // before we start passing out information from WeaponData
    public override void Initiallize(WeaponResource WeaponData, WeaponController weaponController)
    {
        this.WeaponData = WeaponData;
        this.weaponController = weaponController;
    }

    public override void _Ready()
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
        MuzzleFlashRef = GetNode<MuzzleFlash>("./MuzzleFlash");
        if(MuzzleFlashRef == null)
            GD.Print("Empty!");

        /*
        GunSoundEmpty = GetNode<AudioStreamPlayer3D>("GunSoundEmpty");
        */
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
        Shell.Eject(EjectDir, 4.5f);
    }

    public override async void Fire()
    {
        IsFiring = true;

        // Shotgun shoots various pelets
        for(int i = 0; i < 12; i++)
        {
            // Find out if the ray intersected with a body. It will return nothing if not
            Godot.Collections.Dictionary collisionResult = CalculateRay();

            if(collisionResult.Count != 0)
                SpawnDecal((Vector3)collisionResult["position"]);
        }

        weaponController.CameraControllerRef.RequestCameraRecoil();
        weaponController.WeaponRecoilRef.RequestWeaponRecoil();
        MuzzleFlashRef.RequestMuzzleFlash(WeaponData.FireRate);
        // Update Ammo here
        // Gun Sound here
        GunSound.Play();
    
        // Weapon animations should be reactive not authorative in nature
        // Also animation name should be abstracted out to keep it dynamic

        // Fire animation
        WeaponAnimPlayer.Play(WeaponData.Fire.AnimationName, WeaponData.Fire.BlendAmount,FireAnimationSpeed);
        WeaponAnimPlayer.Seek(0.02f, true); // Nudge animation forward to the "kick" pose
        await ToSignal(WeaponAnimPlayer, "animation_finished");
        
        // Pump/rack animation 
        if (WeaponData.Pump != null)
        {
            WeaponAnimPlayer.Play("Pump Animation");
            await ToSignal(WeaponAnimPlayer, "animation_finished");
        }

        IsFiring = false;
    }

    // Shoot a ray cast from the center of the screen
	// straight outwards until it either collides with a body or reaches limit
    private Godot.Collections.Dictionary CalculateRay(float spreadRadiusInPixels = 50.0f, float length = 1000.0f)
    {
		Camera3D camera = Globals.player.WorldCameraController.Camera;
		// Grab the worlds 3D physics state/sandbox. This state is where all of the physics occurs and its handled by the physics server
		var spaceState = camera.GetWorld3D().DirectSpaceState;
	
		Vector2 screenCenter = (Vector2)GetViewport().Get("size") / 2;
		Vector3 originPoint = camera.ProjectRayOrigin(screenCenter);
		Vector3 endPoint = originPoint + camera.ProjectRayNormal(screenCenter) * length;
    
        endPoint.Y += (float)GD.RandRange(-spreadRadiusInPixels*2.0f, spreadRadiusInPixels*2.0f);
        endPoint.X += (float)GD.RandRange(-spreadRadiusInPixels*2.0f, spreadRadiusInPixels*2.0f);

		// Create the ray which will return back a dictionary with metadata on any
		// physics collisions. Make sure to enable collision with bodies or areas
		var query = PhysicsRayQueryParameters3D.Create(originPoint, endPoint);
		query.CollideWithBodies = true;
		query.CollideWithAreas = true;
		query.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2); // Detect layers 1, 2, and 3
		
		// We are essentially creating a dictionary holding a number of keys that pertain to the collision information
		var collisionResult = spaceState.IntersectRay(query);
        return collisionResult;
    }

    private async void SpawnDecal(Vector3 position)
    {
        // This can be offloaded to a seperate decal script
        MeshInstance3D decal = TestDecal.Instantiate<MeshInstance3D>();
        GetTree().Root.AddChild(decal);
        decal.Position = position;

        // TODO: Decal should handle the timer and despawn not the shotgun
        await ToSignal(GetTree().CreateTimer(3.0f), "timeout");
        decal.QueueFree();
    }

    // Could make this abstract so that all guns must implement reload
    public override async void Reload()
    {
        // To prevent spam reloads
        if(IsReloading || IsFiring) 
            return;

        IsReloading = true;
        WeaponAnimPlayer.Play(WeaponData.Reload.AnimationName, WeaponData.Reload.BlendAmount, WeaponData.Reload.AnimationSpeed);
        await ToSignal(WeaponAnimPlayer, "animation_finished");
        IsReloading = false;

    }
}
