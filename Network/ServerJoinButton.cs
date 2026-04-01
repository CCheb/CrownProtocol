using Godot;
using System;

public partial class ServerJoinButton : Control
{	
	[Signal] public delegate void ClientClickedJoinButtonEventHandler(long serverId);
	private Button button;
	private long serverId;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		button = GetNode<Button>("./Button");
		button.Pressed += OnButtonPressed;
	}

	public void AssociateButtonWithServer(long serverId, string serverName)
	{
		this.serverId = serverId;
		button.Text = serverName;
	}

	private void OnButtonPressed()
	{
		EmitSignalClientClickedJoinButton(serverId);
	}
}
