using Godot;
using System;

public partial class GameManager : Node3D
{
	[Export] private Godot.Collections.Array<Marker3D> playerSpawns;
	[Export] private Marker3D spectatorSpawn;
	[Export] private NetworkCore netCore;
	[Export] private PackedScene cameraScene;
	private int connectedPlayers = 0;
	private const int MASTER = 1;

    public override void _ExitTree()
    {
        base._ExitTree();
		GenericCore.Instance.ClientDisconnectedNotifier -= OnPlayerDisconnected;
    }
	public async override void _Ready()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
   		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);

		//TODO: There is still a race condition when everyone loads into the level. Clients should wait for the server before moving on
		if(GenericCore.Instance.IsServer)
			GenericCore.Instance.ClientDisconnectedNotifier += OnPlayerDisconnected;

		GD.Print($"Peer {Multiplayer.GetUniqueId()} GameManager READY");


		GenericCore.Instance.RpcId(1, "SceneReady");
	}

	public void ServerSpawnPlayers()
	{
		int count = 0;
		foreach(var peer in GenericCore.Instance.connectedPeers)
		{
			if(peer.Key != 1)
			{
				netCore.NetCreateObject(0, playerSpawns[count].GlobalPosition, playerSpawns[count].Quaternion, peer.Key);
				// Fill up connectedPlayers here as we it encounters new players. 
				connectedPlayers++;
				count++;
			}
			else
			{
				Camera3D playerCamera = cameraScene.Instantiate<Camera3D>();
				spectatorSpawn.AddChild(playerCamera);
			}
			
		}
	}

	private async void OnPlayerDisconnected(long id)
	{
		connectedPlayers--;
		GD.Print($"Players remaining {connectedPlayers}");

		if(connectedPlayers == 0)
		{
			GD.Print("Returning back to lobby");

			MasterNetworkManager.Instance.RpcId(MASTER, "ChangeGameServerStatus", "INLOBBY");

			// Wait slightly in case there is something else going on
			await ToSignal(GetTree().CreateTimer(0.5), "timeout");

			// Clean up here if any
			GenericCore.Instance.netObjects.Clear();
			GenericCore.Instance.playersLoaded = 0;


			// Now return back to the GameLobby
			GetTree().ChangeSceneToFile("res://Scenes/gameLobby.tscn");
		}
	}
}
