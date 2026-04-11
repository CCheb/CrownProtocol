using Godot;
using System;

public partial interface IBullet
{
    public void Initialize(Transform3D transform);
    public void OnBodyEntered(Node3D Body);

}
