using Godot;
using System;

public partial class PlayerInput : Node
{
    public Vector2 move;
    public float yawDelta;
    public float pitchDelta;
    public bool jump;
    // TODO: Maybe add sprint, and crouch flags
}
