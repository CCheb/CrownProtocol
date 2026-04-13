using Godot;
using System;
using System.Runtime.CompilerServices;


public partial class WarningScreen : Control
{
	private AudioStreamPlayer beep;

	public override void _Ready()
	{
		beep = GetNode<AudioStreamPlayer>("Beep");
	}
	private void OnOkButtonPressed()
	{
		beep.Play();
		Visible = false;
	}
}
