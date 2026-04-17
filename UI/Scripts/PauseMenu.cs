using Godot;
using System;
using System.ComponentModel;

public partial class PauseMenu : Control
{

	[Signal] public delegate void ResumeButtonClickedEventHandler();

	private Control optionsUI;
	private ColorRect confirmScreen;
	private AudioStreamPlayer Beep;

	public override void _Ready()
	{
		base._Ready();
		optionsUI = GetNode<Control>("OptionsTab");
		confirmScreen = GetNode<ColorRect>("ConfirmQuit");
		Beep = GetNode<AudioStreamPlayer>("Beep");
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
		Input.MouseMode = Input.MouseModeEnum.Captured;
		EmitSignalResumeButtonClicked();
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
		// OS.Execute(OS.GetExecutablePath(), new string[] { });
		// GetTree().Quit();
		GenericCore.Instance.DisconnectFromGame();
	}

	public void OnBackPressed()
	{
		Beep.Play();
		Visible = true;
	}
}
