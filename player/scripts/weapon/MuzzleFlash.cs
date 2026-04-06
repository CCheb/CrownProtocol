using Godot;
using System;

public partial class MuzzleFlash : Node3D
{
    // Signal used by the WeaponType to signal it to turn on the muzzle flash
	[Signal] public delegate void MuzzleFlashSignalEventHandler(float flashTime);
	
	private OmniLight3D light;
	private GpuParticles3D emitter;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
        light = GetNode<OmniLight3D>("OmniLight3D");
		emitter = GetNode<GpuParticles3D>("GPUParticles3D");
		MuzzleFlashSignal += AddMuzzleFlash;
    }

	public void RequestMuzzleFlash(float weaponFireRate)
	{
		EmitSignalMuzzleFlashSignal(weaponFireRate);
	}

	public async void AddMuzzleFlash(float flashTime)
    {
		// Make sure light is off in the editor
		light.Visible = true;
		emitter.Emitting = true;
		
		// Flash is really fast!
		await ToSignal(GetTree().CreateTimer(60.0f/flashTime * 0.05f), "timeout");
		light.Visible = false;
    }
}
