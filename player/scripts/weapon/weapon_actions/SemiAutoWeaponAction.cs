using Godot;
using System;

public partial class SemiAutoWeaponAction : IWeaponAction 
{
    private WeaponController weaponController;
    private WeaponBase CurrentWeapon;
    private bool CanFire = true;

    public SemiAutoWeaponAction(WeaponController weaponController, WeaponBase weapon)
    {   
        this.weaponController = weaponController;
        CurrentWeapon = weapon;
    }

    public void OnActionPressed()
    {
        if (!CanFire || CurrentWeapon.IsReloading || CurrentWeapon.IsFiring)
            return;

        weaponController.Rpc("BroadCastShooting", true);
        // The weapon only really cares on how the fire is implemented and needs to be told when to fire 
        CanFire = false;
        CurrentWeapon.Fire();
    }

    public void OnActionReleased()
    {   
        // Need to let go of the trigger before the weapon can shoot again. This is the core
        // of a semi-auto fire mode
        CanFire = true;

        weaponController.Rpc("BroadCastShooting", false);
    }

    // We dont implement anything in Update since we allow the trigger to be pressed and released as many
    // times as possible. We could put a cadence here if needed though
    public void Update(double delta) { }
    
}
