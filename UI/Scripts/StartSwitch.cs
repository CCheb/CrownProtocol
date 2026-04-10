using Godot;
using System;
using System.ComponentModel.Design;

public partial class StartSwitch : Button
{
    // font color
    [Export]
    public Color origFontColor;

    private Tween currentTween;

    private float transitionLength = 0.75f;

    private bool isTweenRunning = false;

    private bool isGray = false;

    public override void _Ready()
    {
        base._Ready();
        Visible = true;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if(isTweenRunning)
        {
            return;
        }
        if(currentTween != null && currentTween.IsRunning())
        {
            currentTween.Kill();
        }
        currentTween = CreateTween();
        if(!isGray)
        {
            currentTween.TweenProperty(this, "theme_override_colors/font_color", Colors.RosyBrown, transitionLength);
            isTweenRunning = true;
            isGray = true;
            ChangeTweenRunning();
        }
        else
        {
            currentTween.TweenProperty(this, "theme_override_colors/font_color", origFontColor, transitionLength);
            isTweenRunning = true;
            isGray = false;
            ChangeTweenRunning();
        }
    }

    private async void ChangeTweenRunning()
    {
        await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
        isTweenRunning = !isTweenRunning;
    }
}
