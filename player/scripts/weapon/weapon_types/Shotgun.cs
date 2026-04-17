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
        Shell.Eject(EjectDir, 4.5f);
    }

    public override async void Fire()
    {
        IsFiring = true;

        (Vector3 originPoint, Vector3 endPoint) projectedRay = ClientCalculateRay();
        PlayFireSequence();
        RpcId(SERVER, MethodName.RequestFire, projectedRay.originPoint, projectedRay.endPoint);
        
        await ToSignal(WeaponAnimPlayer, "animation_finished");
        
        // Pump/rack animation 
        if (WeaponData.Pump != null)
        {
            WeaponAnimPlayer.Play("Pump Animation");
            await ToSignal(WeaponAnimPlayer, "animation_finished");
        }

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

    private void SignalNodes()
    {
        weaponController.CameraControllerRef.RequestCameraRecoil();
        weaponController.WeaponRecoilRef.RequestWeaponRecoil();
        MuzzleFlashRef.RequestMuzzleFlash(WeaponData.FireRate);
    }

    private void UpdateAmmo()
    {
        weaponController.UpdateCurrentWeaponAmmo();
        return;
    }

    private (Vector3, Vector3) ClientCalculateRay(float length = 50.0f)
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
        //TODO: Dont hard code the shotcount
        for(int i = 0; i < 6; i++)
        {
            Godot.Collections.Dictionary collisionResult = ServerCalculateRay(originPoint, endPoint);
            
            if(collisionResult.Count != 0)
            {   
                GD.Print($"Pellet hit at: {collisionResult["position"]}");

                if((Node)collisionResult["collider"] is IEnemy enemy)
                {
                    if(enemy is FPSController player)
                        player.Hit(weaponController.GetCurrentWeaponsDamage(), Multiplayer.GetRemoteSenderId(), player.myNetId.OwnerId);
                    else
                        enemy.Hit(weaponController.GetCurrentWeaponsDamage());
                }

                //SpawnDecal((Vector3)collisionResult["position"]);
            }
        }
    }
    private Godot.Collections.Dictionary ServerCalculateRay(Vector3 originPoint, Vector3 endPoint, float spreadRadiusInPixels = 50.0f)
    {
        var spaceState = GetWorld3D().DirectSpaceState;

        Vector3 randEndPoint = endPoint;
        randEndPoint.Y += (float)GD.RandRange(-spreadRadiusInPixels*2.0f, spreadRadiusInPixels*2.0f);
        randEndPoint.X += (float)GD.RandRange(-spreadRadiusInPixels*2.0f, spreadRadiusInPixels*2.0f);

        var queryCollisions = PhysicsRayQueryParameters3D.Create(originPoint, randEndPoint);
        queryCollisions.CollideWithBodies = true;
		queryCollisions.CollideWithAreas = true;
        queryCollisions.CollisionMask = (1 << 0) | (1 << 1) | (1 << 2); // Detect layers 1, 2, and 3

        var collisionResult = spaceState.IntersectRay(queryCollisions);
        
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
