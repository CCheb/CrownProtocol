using Godot;
using System;

public partial class Leaderboard : Control
{
    private AnimationPlayer anim;

    public override void _Ready()
    {
        anim = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (Input.IsActionJustPressed("tab"))
        {
            anim.Play("popUp");
        }
        else if (Input.IsActionJustReleased("tab"))
        {
            anim.Play("goBack");
        }
    }
    
}
