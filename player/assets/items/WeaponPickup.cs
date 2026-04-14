using Godot;
using System;

public partial class WeaponPickup : ItemPickup
{
    public override void _Ready()
    {
        base._Ready();
    }
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
			player.context.weaponController.OnWeaponPickedUp();

			audio.Play();

			ToggleItem(false);
			await ToSignal(GetTree().CreateTimer(pickupTimeout), "timeout");
			ToggleItem(true);

		}
		
	}
}
