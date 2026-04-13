using Godot;
using System;

public partial class GlowyMove : MeshInstance3D
{
    // For Moving
    private Vector3 StartPos;
    [Export] public Vector3 EndPos;
    private bool reverse = false;
    private bool transitionPhase = false;
    [Export] public float MoveSpeed = 1f;

    // For Growing and Shrinking Y
    private float StartYScale;
    [Export] public float EndYScale = 2.1f;
    [Export] public float ScaleSpeed = 4f;
    private bool reverseScale = false;
    private bool scaleTransitionPhase = true;

    [Export] public bool isGlowyOne = false;
    [Export] public AudioStreamPlayer LaserSound;

    public override void _Ready()
    {
        StartPos = this.GlobalPosition;
        StartYScale = this.Scale.Y;

        SlowStartScale();
    }
    public async void SlowStartScale()
    {
        await ToSignal(GetTree().CreateTimer(0.85f), "timeout");
        scaleTransitionPhase = false;
    }
    public override void _Process(double delta)
    {
        if (reverse && !transitionPhase)
        {
            this.GlobalPosition = this.GlobalPosition.MoveToward(StartPos, MoveSpeed * (float)delta);
            if (this.GlobalPosition.DistanceTo(StartPos) < 0.1f)
            {
                transitionPhase = true;
                ReverseDirection();
            }
        }
        else if(!reverse && !transitionPhase)
        {
            this.GlobalPosition = this.GlobalPosition.MoveToward(EndPos, MoveSpeed * (float)delta);
            if (this.GlobalPosition.DistanceTo(EndPos) < 0.1f)
            {
                transitionPhase = true;
                ReverseDirection();
            }
        }

        if (reverseScale && !scaleTransitionPhase)
        {
            this.Scale = new Vector3(this.Scale.X, Mathf.MoveToward(this.Scale.Y, StartYScale, ScaleSpeed * (float)delta), this.Scale.Z);
            if (Math.Abs(this.Scale.Y - StartYScale) < 0.1f)
            {
                scaleTransitionPhase = true;
                ReverseScale();
            }
        }
        else if(!reverseScale && !scaleTransitionPhase)
        {
            this.Scale = new Vector3(this.Scale.X, Mathf.MoveToward(this.Scale.Y, EndYScale, ScaleSpeed * (float)delta), this.Scale.Z);
            if (Math.Abs(this.Scale.Y - EndYScale) < 0.1f)
            {
                scaleTransitionPhase = true;
                ReverseScale();
            }
        }

    }
    public async void ReverseDirection()
    {
        await ToSignal(GetTree().CreateTimer(1f), "timeout");
        reverse = !reverse;
        transitionPhase = false;
        if (reverse && isGlowyOne)
        {
            LaserSound.Play();
        }
    }
    public async void ReverseScale()
    {
        await ToSignal(GetTree().CreateTimer(1f), "timeout");
        reverseScale = !reverseScale;
        scaleTransitionPhase = false;
    }
}
