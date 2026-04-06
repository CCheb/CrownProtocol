using Godot;
using System;

public abstract partial class CameraLayer : Node
{
    // We must set a contract where each camera layer (input, movement, reload, recoil, etc)
    // must uphold. Since we know that all of them will produce Position and Rotation data for the camera
    // we put those commonalities in an abstract class

    // The => is equivalent to doing a get function from a propety
    // These two variables are already properties.
    public virtual Vector3 PositionOffset => Vector3.Zero;
    public virtual Vector3 RotationOffset => Vector3.Zero;
    
}
