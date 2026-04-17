using Godot;
using System;

public partial class ControlsUi : ColorRect
{
    public void OnBackPressed()
    {
        Visible = !Visible;
    }
}
