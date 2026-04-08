using Godot;
using System;

[GlobalClass]
public partial class AnimationProfile : Resource
{
    [Export] public string AnimationName;
    [Export] public float BlendAmount = -1.0f;
    [Export] public float AnimationSpeed = 1.0f;
}
