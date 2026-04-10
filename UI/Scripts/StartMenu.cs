using Godot;
using System;

public partial class StartMenu : Control
{
    // options ui
    [Export]
    private Control optionsUI;
    [Export]
    private Control tutorialUI;
    [Export]
    private Control CreditsUI;
    [Export]
    private Control ServerUI;
    private bool closed = true;
    [Export]
    private ColorRect FadeScreen;
    [Export]
    private AnimationPlayer animationPlayer;
    [Export]
    private Button startButton;
    [Export]
    private AnimationPlayer MenuAnim;
    private bool isVisible = false;
    private bool isPressing = false;
    [Export] public AudioStreamPlayer Beep;

    public override void _Ready()
    {
        base._Ready();
        optionsUI.Visible = false;
        tutorialUI.Visible = false;
        CreditsUI.Visible = false;
        ServerUI.Visible = false;
        // constrain mouse
        Input.MouseMode = Input.MouseModeEnum.Captured;
        startButton.ButtonPressed = false;
        animationPlayer.Play("fadeOut");
        animationPlayer.AnimationFinished += (StringName animName) =>
        {
            if (animName == "fadeOut")
            {
                FadeScreen.Visible = false;
            }
        };
        MenuAnim.AnimationFinished += (StringName animName) =>
        {
            if (animName == "goBack")
            {
                startButton.Visible = true;
            }
        };
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Check if the event is a left mouse button press
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                isPressing = true;
                // Visually set the button to the 'pressed' state
                startButton.ButtonPressed = true; 
            }
            
            // Left mouse button released (up)
            if (!mouseButton.Pressed && mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (isPressing)
                {
                    isPressing = false;
                    // Visually set the button back to the 'normal' state
                    startButton.ButtonPressed = false;
                    
                    // Emit the signal to trigger the button's action logic
                    startButton.EmitSignal(BaseButton.SignalName.Pressed);
                }
            }
        }
    }

    public void OnStartPressed()
    {
        Beep.Play();
        if(!isVisible)
        {
            Input.MouseMode = Input.MouseModeEnum.Visible;
            isVisible = true;
            MenuAnim.Play("popUp");
            startButton.Visible = false;

        }
        else
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
            isVisible = false;
            MenuAnim.Play("goBack");
        }
    }
    public void OnPlayPressed()
    {
        Beep.Play();
        ServerUI.Visible = !ServerUI.Visible;
    }
    public void OnOptionsPressed()
    {
        Beep.Play();
        optionsUI.Visible = true;
    }
    public void OnTutorialPressed()
    {
        Beep.Play();
        tutorialUI.Visible = true;
    }
    public void OnCreditsPressed()
    {
        Beep.Play();
        if (closed)
        {
            CreditsUI.Visible = true;
            closed = false;
        }
        else
        {
            CreditsUI.Visible = false;
            closed = true;
        }
    }
    public void OnQuitPressed()
    {
        Beep.Play();
        GetTree().Quit();
    }
}
