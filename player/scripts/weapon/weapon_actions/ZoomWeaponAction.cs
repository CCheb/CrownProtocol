using Godot;
using System;

public partial class ZoomWeaponAction : IWeaponAction
{
    private readonly WeaponController weaponController;
    private readonly float desiredZoom;

    private bool isHeld = false;
    public ZoomWeaponAction(WeaponController weaponController, float desiredZoom)
    {
        this.weaponController = weaponController;
        this.desiredZoom = desiredZoom;
    }
    public void OnActionPressed()
    {  
        if(!isHeld)
            weaponController.CameraControllerRef.RequestCameraZoom(desiredZoom);
            
        isHeld = true;
    }
    public void OnActionReleased()
    {   
        if(isHeld)
            weaponController.CameraControllerRef.RequestDeCameraZoom();

        isHeld = false;
    } 

    public void Update(double delta) { }
}
