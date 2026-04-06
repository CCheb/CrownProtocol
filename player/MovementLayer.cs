using Godot;
using System;


public partial class MovementLayer : CameraLayer
{
    [Export] private Node3D OffsetProxy;
    private Vector3 _position;
    private Vector3 _rotation;
    public override Vector3 PositionOffset => _position;
    public override Vector3 RotationOffset => _rotation;

    // Every frame we grab the Proxy's Position and Rotation since its being animated every frame
    // Even if its still we still grab its tranform which will eventually go over to the Camera Contronller
    public override void _Process(double delta)
    {
        base._Process(delta);
        _position = OffsetProxy.Position;
        _rotation = OffsetProxy.Rotation;
    }
}
