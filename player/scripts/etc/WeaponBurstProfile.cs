using Godot;
using System;

[GlobalClass]
public partial class WeaponBurstProfile : Resource
{
    [Export] public int ShotsPerBurst = 3;
    [Export] public float BurstCadence = 0.08f;
}
