using Godot;
using System;

public partial class GameManager : Node3D
{
	[Export] private Marker3D spectatorSpawn;
	[Export] private NetworkCore netCore;
	[Export] private NetworkCore itemSpawner;
	[Export] private MultiplayerSpawner projectileSpawner;
	[Export] private MultiplayerSpawner decalSpawner;
	[Export] private PackedScene cameraScene;
	private Godot.Collections.Array<Marker3D> playerSpawns;
	private Godot.Collections.Array<PickupItemMarker> itemSpawns;
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

		// Windows doesnt like node exports for some reason
		playerSpawns = new Godot.Collections.Array<Marker3D>();
    	foreach (var node in GetTree().GetNodesInGroup("PlayerSpawn"))
    	{
    	    if (node is Marker3D marker)
    	        playerSpawns.Add(marker);
    	}

		itemSpawns = new Godot.Collections.Array<PickupItemMarker>();
		foreach (var node in GetTree().GetNodesInGroup("ItemSpawns"))
		{
			if (node is PickupItemMarker pickUpItem)
				itemSpawns.Add(pickUpItem);
		}

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

	public void ServerSpawnItems()
	{
		foreach (PickupItemMarker item in itemSpawns)
		{
			GD.Print(item);
			GD.Print((int)item.itemToBeSpawned);
			itemSpawner.NetCreateObject((int)item.itemToBeSpawned, item.GlobalPosition, item.Quaternion);
		}
	}

	public void SpawnProjectile(PackedScene projectileScene, Transform3D muzzleRef)
	{
		if (!GenericCore.Instance.IsServer)
			return;

		var projectile = projectileScene.Instantiate();

		if (projectile is IBullet bullet)
		{	
			//var spawnRoot = GetNode(projectileSpawner.SpawnPath);
			AddChild(projectile, true);
			bullet.Initialize(muzzleRef);
		}
		
	}

	public async void SpawnDecal(PackedScene decalScene, Vector3 normal, Vector3 endPoint)
	{
		if (!GenericCore.Instance.IsServer)
			return;

		Decal d = decalScene.Instantiate<Decal>();

		AddChild(d, true);
		d.Position = endPoint;

		// First argument creates a point on the normal of the surface which defines
		// where the object should look. The second argument ensures the decal doesnt roll the wrong way
		d.LookAt(d.GlobalTransform.Origin + normal, Vector3.Up);
		if (normal != Vector3.Up || normal != Vector3.Down)
        {
			d.RotateObjectLocal(new Vector3(1.0f, 0.0f, 0.0f), Mathf.DegToRad(90));
        }
		// Despawn timer
		await ToSignal(GetTree().CreateTimer(3.0f), "timeout");
		// Here we want to create a fade effect. So we create a tween and change linearly interpolate the decals alpha
		var fade = GetTree().CreateTween();
		//var fade2 = GetTree().CreateTween();
		fade.TweenProperty(d, "emission_energy", 0, 1.5);
		//fade2.TweenProperty(d, "modulate:a", 0, 1.5);
		// Need to provide the tween some time for it to interpolate the alpha to 0.0f
		await ToSignal(GetTree().CreateTimer(1.5f), "timeout");
		// Once the timer goes out then we can delete the decal. In this case the decal will
		// only be deleted when its alpha is 0.0f which is basically invisible
		d.QueueFree();
		
	}

	public Node3D RequestRandomPlayerSpawn()
	{
		return playerSpawns[GD.RandRange(0, playerSpawns.Count-1)];
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
