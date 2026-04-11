using Godot;
using System;

public abstract partial class WeaponBase : Node3D
{
    // Need WeaponData to supply vital information to the weapon
    protected WeaponResource WeaponData;
    // Need Controller since we might have to signal information to it
    protected WeaponController weaponController;
    protected AnimationPlayer WeaponAnimPlayer;
    protected AudioStreamPlayer3D GunSound;
    protected AudioStreamPlayer3D GunSoundEmpty;
    protected MuzzleFlash MuzzleFlashRef;
    public  Node3D CameraReloadProxy;
    protected float fireRate;
    protected float FireAnimationSpeed = 1.0f;
    public bool IsReloading = false;
    public bool IsFiring = false;
    public const int SERVER = 1;
    public virtual void Spin(float spinSpeed)
    {
        GD.Print("From Weapon Base!");
    }
    protected void TryPlayingDrawAnimation()
    {
        if(WeaponData != null && WeaponData.Draw != null)
        {
            WeaponAnimPlayer.Play(WeaponData.Draw.AnimationName, WeaponData.Draw.BlendAmount, WeaponData.Draw.AnimationSpeed);
            GD.Print("Played");
        }
    }
    protected float CalculateFireAnimationSpeed()
    {
        // Want the weapon's fire animation to play fast enough to finish before the next shot
        float FireAnimLength = WeaponAnimPlayer.GetAnimation(WeaponData.Fire.AnimationName).Length;
        float RoundsPerSecond = WeaponData.FireRate / 60.0f;
        float desiredInterval = 1f / RoundsPerSecond;
        return Mathf.Min(FireAnimLength / desiredInterval, 2.0f);
    }
    public abstract void Initiallize(WeaponResource WeaponData, WeaponController Controller);
    public abstract void Fire();
    public abstract void Reload();
    

}
