using Godot;
using System;

public partial class PickupItemMarker : Marker3D
{
	[Export] public Globals.PickupItems itemToBeSpawned;
}
