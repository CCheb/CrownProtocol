using Godot;
using System;

public static partial class FireModeFactory
{
    public static IWeaponAction CreateNewWeaponAction(WeaponController weaponController, WeaponResource WeaponData, WeaponBase CurrentWeapon, Globals.WeaponActions CurrentWeaponAction)
    {
        switch(CurrentWeaponAction)
        {
            // Based on the CurrentFireMode we return the appropriate FireMode object. If the WeaponResource specified
            // FireMode Shotgun then the factory will return the ShotgunFireMode and so on
            case Globals.WeaponActions.FullAuto :
                return new FullAutoWeaponAction(CurrentWeapon, WeaponData.FireRate);
            case Globals.WeaponActions.SemiAuto :
                return new SemiAutoWeaponAction(CurrentWeapon);
            case Globals.WeaponActions.BurstFire:
                return new BurstFireWeaponAction(CurrentWeapon, WeaponData.burstProfile);
            case Globals.WeaponActions.Zoom :
                return new ZoomWeaponAction(weaponController, WeaponData.desiredZoom);
            case Globals.WeaponActions.Spin:
                return new SpinWeaponAction(CurrentWeapon);
            case Globals.WeaponActions.NoAction :
                return null;
            default :
                throw null;
        }
    }
}
