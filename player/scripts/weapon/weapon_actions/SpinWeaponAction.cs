using Godot;
using System;

public partial class SpinWeaponAction : IWeaponAction
{
    private readonly WeaponBase currentWeapon;
    private float currentSpin = 0.0f;
    private float targetSpin = 0.0f;
    private bool isHeld = false;
    public SpinWeaponAction(WeaponBase currentWeapon)
    {
        this.currentWeapon = currentWeapon;
    }

    public void OnActionPressed()
    {
        if(!isHeld)
            targetSpin = 3.0f;

        
        isHeld = true;
    }

    public void OnActionReleased()
    {  
        if(isHeld)
            targetSpin = 0.0f;
        
        isHeld = false;
    }

    public void Update(double delta)
    {
        currentSpin = Mathf.Lerp(currentSpin, targetSpin, 2f*(float)delta);
        currentWeapon.Spin(currentSpin);
    }
}
