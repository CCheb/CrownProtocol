using Godot;
using System;

public partial class WeaponCamera : Camera3D
{
    [Export] public Camera3D WorldCamera;

    public override void _Process(double delta)
    {
        // Must have the weapon camera follow the main camera
        // this is not as expensive as you think. Having a second
        // camera is whats preventing weapon clipping
        //base._Process(delta);
        GlobalTransform = WorldCamera.GlobalTransform;
        Fov = WorldCamera.Fov;
    }
}
