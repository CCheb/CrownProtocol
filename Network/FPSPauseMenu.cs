using Godot;
using System;

public partial class FPSPauseMenu : Control
{   
    [Signal] public delegate void ResumeButtonClickedEventHandler();
    public void UnHideMenu()
    {
        Input.MouseMode = Input.MouseModeEnum.Visible;
        Visible = true;
    }

    public void OnResumeButtonClicked()
    {
        Input.MouseMode = Input.MouseModeEnum.Captured;
        Visible = false;
        EmitSignalResumeButtonClicked();
    }

    public void OnQuitButtonClicked()
    {
        // Could signal here to the NetworkPlayer to clean up before proceeding
        GenericCore.Instance.DisconnectFromGame();
    }
}
