using Godot;
using System;
using System.ComponentModel;

public static class WeaponFactory 
{
	public static WeaponBase Create(WeaponResource WeaponData, WeaponController Controller)
	{
		WeaponBase NewWeapon;
		// By instantiating any weapon type, its _Ready() will be called which will in turn instantiate its corresponding weapon scene
		switch (WeaponData.WeaponType)
		{
			case Globals.WeaponTypes.Hitscan : 
				NewWeapon = WeaponData.WeaponScene.Instantiate<Hitscan>();
				// Order goes Instantiate -> Initialize -> AddChild -> _Ready()
				// We need to Intiallize since we need to initiallize the WeaponData and the weapon Controller before hitting ready
				NewWeapon.Initiallize(WeaponData, Controller);
				return NewWeapon;
			case Globals.WeaponTypes.Projectile :
				NewWeapon = WeaponData.WeaponScene.Instantiate<Projectile>();
				NewWeapon.Initiallize(WeaponData, Controller);
				return NewWeapon;
			case Globals.WeaponTypes.Shotgun :
				NewWeapon = WeaponData.WeaponScene.Instantiate<Shotgun>();
				NewWeapon.Initiallize(WeaponData, Controller);
				return NewWeapon;
			case Globals.WeaponTypes.Spin :
				NewWeapon = WeaponData.WeaponScene.Instantiate<Spinup>();
				NewWeapon.Initiallize(WeaponData, Controller);
				return NewWeapon;
			default : return null;
		}
	}
}
