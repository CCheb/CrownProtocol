using Godot;
using System;
using System.Resources;

public partial class Globals : Node
{
    [Signal] public delegate void PlayerReadyEventHandler();
	// Reference to player conponent. This will act as our singleton
    // Its static here since we dont want to create a Globals object
    static public FPSController player { get; set; }

    public enum WeaponTypes
    {
        Hitscan,
        Projectile,
        Shotgun,
        Beam,
        Spin
    }

    public enum WeaponActions
    {
        FullAuto,
        SemiAuto,
        BurstFire,
        Zoom,
        Spin,
        NoAction
    }

    public enum MovementStates
    {
        Idle,
        Walk,
        Sprint,
        Slide,
        Crouch,
        Jump,
        Fall
    }

    public struct WeaponMovementProfle
    {
        public bool IsIdle;
        public float BobSpeed;
        public float HorizontalBobAmount;
        public float VerticalBobAmount;
    }

    public enum PickupItems
    {
        Smg,
        BurstRifle,
        Shotgun,
        Bazooka,
        Sniper,
        Health,
        Armor,
    }


}
