using Godot;
using System;

public partial class WeaponPickup : ItemPickup
{
	[Export] private Globals.PickupItems desiredPickup;
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		base._Process(delta);
	}

	public override async void OnBodyEntered(Node3D body)
	{
		if(!GenericCore.Instance.IsServer)
			return;

		if(body is FPSController player)
		{	
			// PickupItems must match with the Arsenal ordering or else the wrong weapon will be loaded in!
			player.context.weaponController.OnWeaponPickedUp((int)desiredPickup+1); // +1 to offset the pistol!

			audio.Play();

			ToggleItem(false);
			await ToSignal(GetTree().CreateTimer(pickupTimeout), "timeout");
			ToggleItem(true);
		}
	}
}
