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
        if (!weaponController.context.player.myNetId.IsLocal)
            return;

        ShellEjection Shell = ShellCasingScene.Instantiate<ShellEjection>();
        GetTree().CurrentScene.AddChild(Shell);
        Shell.GlobalTransform = ShellEjectionMarker.GlobalTransform;
        Vector3 EjectDir = ShellEjectionMarker.GlobalTransform.Basis.X.Normalized();
        Shell.Eject(EjectDir, 0.3f);
    }

     public override async void Fire()
    {
        IsFiring = true;

        (Vector3 originPoint, Vector3 endPoint) projectedRay = ClientCalculateRay();
        PlayFireSequence();
        RpcId(SERVER, MethodName.RequestFire, projectedRay.originPoint, projectedRay.endPoint);

        await ToSignal(WeaponAnimPlayer, "animation_finished");

        IsFiring = false;
    }

    private void PlayFireSequence()
    {
        SignalNodes();
        UpdateAmmo();
        WeaponAnimPlayer.Play(WeaponData.Fire.AnimationName, WeaponData.Fire.BlendAmount,FireAnimationSpeed);
        WeaponAnimPlayer.Seek(0.02f, true); // Nudge animation forward to the "kick" pose
        GunSound.GlobalTransform = weaponController.context.player.GlobalTransform;
        GunSound.Play();
    }
    
    private (Vector3, Vector3) ClientCalculateRay(float length = 1000.0f)
    {
        Camera3D camera = weaponController.context.cameraController.Camera;
		
		Vector2 screenCenter = (Vector2)GetViewport().Get("size") / 2;
		
		Vector3 originPoint = camera.ProjectRayOrigin(screenCenter);
		Vector3 endPoint = originPoint + camera.ProjectRayNormal(screenCenter) * length;

        return (originPoint, endPoint);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
    private void RequestFire(Vector3 originPoint, Vector3 endPoint)
    {
        if (!GenericCore.Instance.IsServer)
            return;
        
        // TODO: Verify points are good here

        Godot.Collections.Dictionary collisionResult = ServerCalculateRay(originPoint, endPoint);

        if (collisionResult.Count != 0)
        {
            //GD.Print($"Hitscan hit at: {collisionResult["position"]}, {collisionResult["collider"]}");
        }
    }

    private Godot.Collections.Dictionary ServerCalculateRay(Vector3 originPoint, Vector3 endPoint)
    {
        var spaceState = GetWorld3D().DirectSpaceState;

        var queryCollisions = PhysicsRayQueryParameters3D.Create(originPoint, endPoint);
        queryCollisions.CollideWithBodies = true;
		queryCollisions.CollideWithAreas = true;
		queryCollisions.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2); // Detect layers 1, 2, and 3

        var collisionResult = spaceState.IntersectRay(queryCollisions);
        
        return collisionResult;
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
