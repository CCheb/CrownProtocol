using Godot;
using System;

public partial interface IBullet
{
    public void Initialize(Transform3D transform, long ownerId);
    public void OnBodyEntered(Node3D Body);

}
