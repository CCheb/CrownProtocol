using Godot;
using System;
using System.ComponentModel;

public partial class CameraController : Node3D
{   
    /*
    Camera Controllers job is to take all of the camera data produced by the Camera Layers and combine them to produce one 
    final camera position and rotation. Think of the layers as pipes connected to a central processing center where the water is filtered and cleaned
    */

    // TODO: create an API for the CameraController so that the WeaponController doesnt access Layer nodes directly
    public Camera3D Camera {get => _camera;}
    [Export] private Camera3D _camera;
    // InputCameraLayer handles basic player input such as mouse movement
    [Export] private CameraLayer InputCameraLayer;
    // MovementLayer handles camera movement based on the movement state machine/system. e.g. sprint state specifies a camera animation
    [Export] private CameraLayer MovementLayer;
    // CameraRecoilLayer handles weapon specific recoil which for the camera is just rotations. This node gets signaled by the WeaponController
    [Export] private CameraLayer cameraRecoilLayer;
    // CameraReload handles weapon specific reload animations which are exported from the weapon itself to the WeaponController
    [Export] private CameraLayer CameraReload;
    // CameraJumpingLayer that dynamic adds a slight roll to the finalRotation depending on how hard the the player hit the ground
    [Export] public  CameraLayer CameraJumpingLayer;
    [Export] public  CameraZoomLayer cameraZoomLayer;
    // BaseOffset helps in giving us an origin/base to apply all of the camera translations
    [Export] private Vector3 BaseOffset = new Vector3(0.0f, 1.428f, 0.0f);
    
    private PlayerContext context;

    public void SetContext(PlayerContext ctx)
    {
        context = ctx;
    }

    public override void _Ready()
    {
        base._Ready();
       
        SetProcess(false);

        context.player.PlayerReady += OnPlayerReady;   
    }

    private void OnPlayerReady()
    {
        SetProcess(true);

        GD.Print("CameraController Ready!");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (!context.player.myNetId.IsLocal)    // This might not be ready by the time it executes
            return;

        // Its essential that we reset these values every frame. If we left these two outside then the offsets would stack
        // which is not something we would like. Since _Process happens so fast these calculation feel smooth when in-game

        // Notice how finalPosition if initially assigned the BaseOffset which helps specify the CameraController to the right position every frame
        Vector3 finalPosition = BaseOffset;
        Vector3 finalRotation = Rotation;
        float finalFov = 0.0f;

        /*
        if(InputCameraLayer != null)
        {
            // InputCameraLayer is special since it acts as the base rotation that we would like to keep when the player stops looking around.
            // Its a base rotation that everything else applies on top of. Every frame we start at a base rotation 
            finalRotation = InputCameraLayer.RotationOffset;
        }
        */

        // All of the layers that are considered offsets MUST BE ADDED to the final transform. If we used = then they would take over
        if(MovementLayer != null)
        {   
            // The main reason we do a += for the this and subsequent layers is because the data produced by them is temporary in nature
            // and can thus be considered offsets. In this case we take the movement layers Position offset if any and apply its changes every frame
            finalPosition += MovementLayer.PositionOffset;
            finalRotation += MovementLayer.RotationOffset;
        }

        if(cameraRecoilLayer != null)
        {
            // When the player fires the weapon the CameraRecoilLayer gets signaled and lerps a camera rotation over time which is perfect for this system
            finalRotation += cameraRecoilLayer.RotationOffset;
        }

        if(CameraReload != null)
        {   
            // We apply the CameraReload rotation to the finalRotation
            finalRotation += CameraReload.RotationOffset;
        }

        if(CameraJumpingLayer != null)
        {   
            // We apply the CameraJumping roation to the finalRotation. This should add a slight roll
            finalRotation += CameraJumpingLayer.RotationOffset;
        }

        if(cameraZoomLayer != null)
        {
            finalFov = cameraZoomLayer.currentFov;
        }

        
        // If applied to itself the camera will follow. It must stay dumb
        // Final camera rotation = base look + effects (recoil, reload, movement, etc)
        Position = finalPosition;
        Basis = Basis.FromEuler(finalRotation);
        _camera.Fov = finalFov;
    }

    // Each weapon has its own ReloadProxy and thus when the player switches to another weapon the WeaponController will notify the CameraController
    // of the new ReloadProxy
    public void SetCameraReloadLayer(Node3D Proxy)
    {
        if(CameraReload is CameraReloadLayer RL)
        {
           RL.SetProxy(Proxy);
        }
    }

    public void RequestCameraZoom(float desiredZoom)
    {
        cameraZoomLayer.EmitSignal("AddCameraZoom", desiredZoom);
    }

    public void RequestDeCameraZoom()
    {
        cameraZoomLayer.EmitSignal("RemoveCameraZoom");
    }

    public void RequestCameraRecoil()
    {
        cameraRecoilLayer.EmitSignal("AddCameraRecoilSignal");
    }

    public void SetCameraRecoilProperties(Vector3 recoilAmmount, float snapAmmount, float speed)
    {
        ((CameraRecoilLayer)cameraRecoilLayer).SetCameraRecoilProperties(
            recoilAmmount, snapAmmount, speed);
    }

    
}
