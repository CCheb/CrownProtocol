using Godot;
using System;


public partial class CameraRecoilProxy : Node3D
{
    
    public override void _Ready()
    {
        base._Ready();
    }

    private void OnWeaponDataChanged()
    {
        GD.Print("Node picked that up!");
    }
}
