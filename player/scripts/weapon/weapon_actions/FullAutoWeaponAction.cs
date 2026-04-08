using Godot;
using System;

public partial class FullAutoWeaponAction : IWeaponAction
{
    private WeaponBase CurrentWeapon;
    private bool IsHeld = false;
    private float Timer = 0.0f;
    private float Interval = 0.0f;
    public FullAutoWeaponAction(WeaponBase CurrentWeapon, float FireRate)
    {
        this.CurrentWeapon = CurrentWeapon;
        // Fire rate is in terms of RPM (Rounds Per Minute)
        Interval = 60.0f/FireRate;
    }
    public void OnActionPressed()
    {
        IsHeld = true;

        // This function is only triggered once the button is first pressed
        // Because of that we can call firing here to give the player immediate feedback
        // when they first start shooting and prevent a sloppy delay
        if(!CurrentWeapon.IsReloading)
        {
            CurrentWeapon.Fire();
            Timer = Interval;
        }
    } 

    public void OnActionReleased() => IsHeld = false;

    public void Update(double delta)
    {
        // Very simple Full auto cadence where we can only fire the weapon once per frame
        // The other way we could have done this is via a simple async function just like how
        // we did it in planechaser but this version is much more robust since it involves delta
        if(!IsHeld || CurrentWeapon.IsReloading)
            return;

        Timer -= (float)delta;
        if(Timer <= 0)
        {
            CurrentWeapon.Fire();
            Timer += Interval;
        }
    }
}
