using Godot;
using System;

public partial class AmmoCounter : Label
{
	// Signal for updating the ammo count when shooting
	[Signal] public delegate void UpdateAmmoCountSignalEventHandler(int AmmoCount);
	// Signal for when swaping weapons 
	[Signal] public delegate void SwapWeaponAmmoCountSignalEventHandler(int AmmoCount);
	[Export] private WeaponController weaponController;
	private int TargetFontSize;
	private int DefaultFontSize;

	// Called when the node enters the scene tree for the first time.
	public async override void _Ready()
	{
		DefaultFontSize = GetThemeFontSize("font_size");
		TargetFontSize = DefaultFontSize;

		await ToSignal(weaponController, "ready");


		UpdateAmmoCountSignal += AmmoSpent;
		SwapWeaponAmmoCountSignal += UpdateText;

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		TargetFontSize = (int)Mathf.Lerp(TargetFontSize, DefaultFontSize, 0.1f);
		AddThemeFontSizeOverride("font_size", TargetFontSize);
	}

	public void AmmoSpent(int ammoCount)
	{
		TargetFontSize = DefaultFontSize * 2;
		UpdateText(ammoCount);
	}

	public void UpdateText(int ammoCount)
	{
		Text = ammoCount > 0 ? ammoCount.ToString() : new string("OUT!");
	}
}
