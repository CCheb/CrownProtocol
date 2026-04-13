using Godot;
using System;

public partial class OptionsScript : Control
{
	private Label volumeLabel;
	private Slider volumeSlider;
	[Export] public Label FOVLabel;
	[Export] public Slider FOVSlider;



	private OptionButton displayModeOption;
	[Export] public AudioStreamPlayer Beep;

	public override void _Ready()
	{
		base._Ready();
		volumeLabel = GetNode<Label>("PanelContainer/ColorRect/VolumeLabel");
		ProcessMode = ProcessModeEnum.Always;

		volumeSlider = GetNode<Slider>("PanelContainer/ColorRect/VolumeSlider");
		displayModeOption = GetNode<OptionButton>("PanelContainer/ColorRect/DisplayOptionBar");

		GrabCurrentSettings();
	}

	private void OnBackPressed()
	{
		Beep.Play();
		Visible = false;
	}

	private void OnVolumeSliderChanged(float value)
	{
		volumeLabel.Text = ((int)value).ToString() + "%";
		AudioServer.SetBusVolumeDb(AudioServer.GetBusIndex("Master"), Mathf.LinearToDb(value / 100f));
	}

	private void OnDisplayItemSelected(int index)
	{
		Beep.Play();
		if (index == 0)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
		}

		else if (index == 1)
		{
			DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);
		}
	}
	
	private void OnFOVSliderChanged(float value)
	{
		FOVLabel.Text = ((int)value).ToString();
		Camera3D camera = GetTree().GetFirstNodeInGroup("CAMERA") as Camera3D;
		if (camera != null)
		{
			camera.Fov = value;
		}
	}

	private void GrabCurrentSettings()
	{
		// Volume
		float dbVolume = AudioServer.GetBusVolumeDb(AudioServer.GetBusIndex("Master"));
		float linearVolume = Mathf.DbToLinear(dbVolume) * 100f;
		volumeSlider.Value = linearVolume;
		volumeLabel.Text = ((int)linearVolume).ToString() + "%";

		// Display Mode
		var mode = DisplayServer.WindowGetMode();
		if (mode == DisplayServer.WindowMode.Windowed)
		{
			displayModeOption.Selected = 0;
		}
		else if (mode == DisplayServer.WindowMode.Fullscreen)
		{
			displayModeOption.Selected = 1;
		}
		
		// FOV
		Camera3D camera = GetTree().GetFirstNodeInGroup("CAMERA") as Camera3D;
		if (camera != null)
		{
			FOVSlider.Value = camera.Fov;
			FOVLabel.Text = ((int)camera.Fov).ToString();
		}
	}
	
}
