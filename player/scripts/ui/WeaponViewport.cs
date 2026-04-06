using Godot;
using System;

public partial class WeaponViewport : SubViewport
{
	public override void _Ready()
    {
        UpdateSize();
        GetWindow().SizeChanged += UpdateSize;
    }

    private void UpdateSize()
    {
        Vector2I size = (Vector2I)GetViewport().GetVisibleRect().Size;
        Size = size;
    }
}
