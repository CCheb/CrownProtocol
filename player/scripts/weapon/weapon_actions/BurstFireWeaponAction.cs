using Godot;
using System;

public partial class BurstFireWeaponAction : IWeaponAction
{
    private WeaponController weaponController;
    private WeaponBase CurrentWeapon;
    private bool CanFire = true;
    private int shotsPerBurst = 3;
    private float burstCadence = 0.08f;

    public BurstFireWeaponAction(WeaponController weaponController, WeaponBase weapon, WeaponBurstProfile burstProfile)
    {
        this.weaponController = weaponController;
        CurrentWeapon = weapon;
        shotsPerBurst = burstProfile.ShotsPerBurst;
        burstCadence = burstProfile.BurstCadence;
    }

    public async void OnActionPressed()
    {
        if (!CanFire || CurrentWeapon.IsReloading || CurrentWeapon.IsFiring)
            return;

        weaponController.Rpc("BroadCastShooting", true);
        // The weapon only really cares on how the fire is implemented and needs to be told when to fire 
        CanFire = false;
        for(int i = 0; i < shotsPerBurst; i++)
        {
            CurrentWeapon.Fire();
            await CurrentWeapon.ToSignal(CurrentWeapon.GetTree().CreateTimer(burstCadence), "timeout");
        }
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
