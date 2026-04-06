using Godot;
using System;

[GlobalClass]
public partial class WeaponResource : Resource
{
    // The idea is to have each weapon fill out this resource
    // since each of them will be different in transform, mesh, etc
    // This is the cartrige that gets inserted into the Weapon 'console'
    // we can always swap cartriges in and out
    
    [Export] public string WeaponName;
    [Export] public Globals.WeaponTypes WeaponType;
    [Export] public Globals.WeaponActions PrimaryWeaponAction;
    [Export] public Globals.WeaponActions SecondaryWeaponAction;

    [ExportGroup("Weapon Transform")]
    [Export] public Vector3 ViewportPosition;
    [Export] public Vector3 ViewportRotation;
    [Export] public Vector3 ViewportScale = new Vector3(1.0f, 1.0f, 1.0f);

    [ExportGroup("Weapon Sway")]
    // How much you can sway left and right
    [Export] public Vector2 MouseSwayMin = new Vector2(-20.0f, -20.0f);
    [Export] public Vector2 MouseSwayMax = new Vector2(20.0f, 20.0f);
    [Export(PropertyHint.Range, "0, 0.2, 0.01")] public float PositionSwaySpeed = 0.07f;
    [Export(PropertyHint.Range, "0, 0.2, 0.01")] public float RotationSwaySpeed = 0.1f;
    [Export(PropertyHint.Range, "0, 0.50, 0.01")] public float MouseInputPositionOffset = 0.1f;
    [Export(PropertyHint.Range, "0, 0.50, 0.1")] public float MouseInputRotationAmount = 30.0f;

    [ExportGroup("Random Idle Sway")]
    [Export] public float IdleSwayAdjustment = 10.0f;
    [Export] public float IdleSwayRotationStength = 300.0f;
    [Export] public float IdleSwayAmmount = 5.0f;
    [Export] public float IdleSwaySpeed = 1.2f;

    [ExportGroup("Visual Settings")]
    [Export] public PackedScene WeaponScene;

    [ExportGroup("Camera Recoil")]
    [Export] public Vector3 CameraRecoilAmount = new Vector3(0.15f, 0.05f, 0.0f);
    [Export] public float CameraSnapAmount = 8.0f;
    [Export] public float CameraRecoverySpeed = 4.0f;
    
    [ExportGroup("Weapon Recoil")]
    [Export] public Vector3 WeaponRecoilAmount = new Vector3(0.01f, 0.01f, 0.3f);
    [Export] public float WeaponSnapAmount = 10.0f;
    [Export] public float WeaponRecoverySpeed = 20.0f;

    [ExportGroup("Weapon Stats")]
    [Export] public float FireRate = 550.0f;
    [Export] public float desiredZoom = 50.0f;
    [Export] public int AmmoCount = 0;
    [Export] public int AmmoCapacity = 0;

    [ExportGroup("Special Weapon Stats")]
    [Export] public WeaponBurstProfile burstProfile = new();

    [ExportGroup("Animations")]
    [Export] public AnimationProfile Fire;
    [Export] public AnimationProfile Reload;
    [Export] public AnimationProfile Draw = null;
    [Export] public AnimationProfile Pump = null;

}
