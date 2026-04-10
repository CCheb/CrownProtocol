using Godot;
using System;
using System.ComponentModel;

public partial class PauseMenu : Control
{
	[Export]
	private Control optionsUI;
	[Export] private ColorRect confirmScreen;
	[Export] public AudioStreamPlayer Beep;

	public override void _Ready()
	{
		base._Ready();
		optionsUI.Visible = false;
		confirmScreen.Visible = false;
		Visible = false;
		ProcessMode = ProcessModeEnum.Always;
	}

	public override void _Process(double delta)
	{
		if (Input.IsActionJustPressed("exit") && !optionsUI.Visible)
		{
			Visible = !Visible;
			optionsUI.Visible = false;

			// change mouse mode
			if (Input.MouseMode == Input.MouseModeEnum.Captured)
			{
				Input.MouseMode = Input.MouseModeEnum.Visible;
			}
			else
			{
				Input.MouseMode = Input.MouseModeEnum.Captured;
			}
		}
	}

	public void OnContinuePressed()
	{
		Beep.Play();
		Visible = false;
		GetTree().Paused = false;
		Input.MouseMode = Input.MouseModeEnum.Captured;
	}

	public void OnOptionsPressed()
	{
		Beep.Play();
		optionsUI.Visible = true;
		Visible = false;
	}

	public void OnControlsPressed()
	{
		Beep.Play();
		//TODO: controls screen
	}

	public void OnQuitPressed()
	{
		Beep.Play();
		confirmScreen.Visible = !confirmScreen.Visible;
	}

	public void OnConfirmPressed()
	{
		Beep.Play();
		OS.Execute(OS.GetExecutablePath(), new string[] { });
		GetTree().Quit();
	}

	public void OnBackPressed()
	{
		Beep.Play();
		Visible = true;
	}
}
