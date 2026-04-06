using Godot;
using System;

// An interface is a contract in which any class that subscribes to this interface is bound to implement its functions
// This differs from an abstract class where some amount of state and implementation is provided to child classes. An
// interface is not a class and mearly provides a condition/contract to uphold
public interface IWeaponAction
{
    // In this case this interface serves to guide the associated Weapon type on when to fire but does not know when to fire
    // or how the fire is actually implemented (hitscan, projectile, multi-ray, etc)
    void OnActionPressed();
    void OnActionReleased();

    // Provides the timing/cadence especially for FullAuto firing
    void Update(double delta);
    
}
