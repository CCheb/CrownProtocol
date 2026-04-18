using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

public partial class GameLobby : Control
{
	[Export] private GridContainer lobby; 
	[Export] private Label countDownLabel;
	[Export] private NetworkCore netCore;
	private Godot.Collections.Array<Sprite2D> levelImages;
	private short currentLevelImageIndex = 0;
	private short clientsConnected = 0;
	private short countDown = 5;
	private bool gameStarted = false;
	private bool countdownRunning = false;
	private const int MASTER = 1;
	private AudioStreamPlayer beep;
	public override void _ExitTree()
    {
        base._ExitTree();
		
		foreach(UserNpm player in lobby.GetChildren())
		{
			if(player != null)
				player.ReadyUpToggled -= OnPlayerPressedReady;
		}
    }

    public override void _Ready()
    {
        base._Ready();

		lobby = GetNodeOrNull<GridContainer>("./GridContainer");
		netCore = GetNodeOrNull<NetworkCore>("./MultiplayerSpawner");
		beep = GetNode<AudioStreamPlayer>("Beep");

		// Grab level images
		levelImages = new Godot.Collections.Array<Sprite2D>();

		foreach (Sprite2D image in GetTree().GetNodesInGroup("LevelImages"))
		{
			levelImages.Add(image);
		}

		netCore.PlayerJoined += OnPlayerJoined;
		GenericCore.Instance.FirstClient += OnFirstClientConnected;
    }

	private void OnFirstClientConnected()
	{
		if(!GenericCore.Instance.IsServer && GenericCore.Instance.isFirstClientToJoin)
		{
			SetUpPopupMenu();
		}
	}

	private void SetUpPopupMenu()
	{
		var popup = GetNode<MenuButton>("MenuBackground/MenuButton").GetPopup();

		popup.IdPressed += OnPopupPressed;

		GetNode<ColorRect>("MenuBackground").Visible = true;
	}

	private void OnPopupPressed(long id)
	{
		// Only first connected client will be able to run this
		beep.Play();
		RpcId(1, MethodName.RequestLevelImageChange, (short)id);
	}


	private void OnQuitButtonPressed()
	{
		beep.Play();
		GenericCore.Instance.DisconnectFromGame();
	}

	[Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = false,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestLevelImageChange(short newLevelIndex = 0)
	{
		if(GenericCore.Instance.IsServer)
		{
			// Swap with new
			levelImages[currentLevelImageIndex].Visible = false;
			levelImages[newLevelIndex].Visible = true;
			currentLevelImageIndex = newLevelIndex;
			Rpc(MethodName.BroadcastLevelPath, newLevelIndex);
		}
	}

	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void BroadcastLevelPath(short newLevelIndex)
	{
		// Its necessary for all the peers to know what level to load into
		switch (newLevelIndex)
		{
			case 0:
				GenericCore.Instance.levelPath = new string($"res://Scenes/spaceLevel.tscn");
				break;
			case 1:
				GenericCore.Instance.levelPath = new string($"res://Scenes/desertLevel.tscn");
				break;
			case 2:
				GenericCore.Instance.levelPath = new string($"res://Scenes/factoryLevel.tscn");
				break;

		}
	}
	[Rpc(MultiplayerApi.RpcMode.Authority,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void BroadcastLevelPathForStart(String path)
	{
		// resend level path before starting
		switch (path)
		{
			case "res://Scenes/spaceLevel.tscn":
				GenericCore.Instance.levelPath = new string($"res://Scenes/spaceLevel.tscn");
				break;
			case "res://Scenes/desertLevel.tscn":
				GenericCore.Instance.levelPath = new string($"res://Scenes/desertLevel.tscn");
				break;
			case "res://Scenes/factoryLevel.tscn":
				GenericCore.Instance.levelPath = new string($"res://Scenes/factoryLevel.tscn");
				break;

		}
	}
	
	private void OnPlayerJoined(Node node)
	{
		GD.Print("Its Working");
		if(GenericCore.Instance.IsServer)
		{
			clientsConnected++;
			if(node is UserNpm playerCard)
			{
				GD.Print("Signal connected!!!");
				playerCard.ReadyUpToggled += OnPlayerPressedReady;
			}
		}
	}	

	private void OnPlayerLeft()
	{	
		// Watch this
		if(GenericCore.Instance.IsServer)
			clientsConnected--;
	}

	private void OnPlayerPressedReady()
	{
		CheckLobbyState();
	}

	private async void CheckLobbyState()
	{
		if (!GenericCore.Instance.IsServer)
        	return;

    	if (clientsConnected < 2 || gameStarted)
    	    return;
	
    	if (!AllPlayersReady())	// Need All players to press ready before proceeding
    	{
    	    ResetCountDown();
    	    return;
    	}
	
    	if (countdownRunning)	// so that it doesnt loop again!
    	    return;
	
    	countdownRunning = true;
    	while (countDown >= 0)
    	{
			countDownLabel.Visible = true;
    		countDownLabel.Text = countDown.ToString();
    		await ToSignal(GetTree().CreateTimer(1.0f), "timeout");

    		if (!AllPlayersReady())
    		{
    		    ResetCountDown();
    		    countdownRunning = false;
    		    return;
    		}

    		countDown--;
    	}
		Rpc(MethodName.BroadcastLevelPathForStart, GenericCore.Instance.levelPath);
    	gameStarted = true;

    	GD.Print("Game Started!");
		
		LoadGame();
	}

	private bool AllPlayersReady()
	{	
		// Assume everyone is ready 
		bool lobbyReady = true;
		foreach(UserNpm player in lobby.GetChildren())
		{	
			if(player == null)	
				break;
	
			if(!player.IsReady)
			{
				lobbyReady = false;
				break;
			}
		}

		return lobbyReady;
	}

	private void ResetCountDown()
	{
		countDown = 5;
		countDownLabel.Text = countDown.ToString();
		countDownLabel.Visible = false;
	}

	private void LoadGame()
	{
		Rpc(MethodName.ChangeToLoadingScreen, "res://Scenes/loadingScreen.tscn");
	}
	
	[Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private async void ChangeToLoadingScreen(string gameScenePath)
	{
	    GD.Print($"Peer {Multiplayer.GetRemoteSenderId()} signaled peer {Multiplayer.GetUniqueId()} to LoadGame");
		
	    if (GenericCore.Instance.IsServer)
	    {	
			DiscardNetworkObjects();
			// Set your status to busy
			MasterNetworkManager.Instance.RpcId(MASTER, "ChangeGameServerStatus", "INGAME");
	    } 
		
		// Since clients dont have netObjects we could also wait for the server before proceeding. [THis could break]
		await WaitForXFrames(4);

	    GetTree().ChangeSceneToFile(gameScenePath);
	}

	private void DiscardNetworkObjects()
	{
		// Server is the only peer that can see the valid _netObjects
	    var objects = GenericCore.Instance.netObjects.Values.ToList();
	    foreach (var netId in objects)
	    {
	        if (netId != null && IsInstanceValid(netId) && netId.GetParent() != null)
	        {
	            netId.GetParent().QueueFree();
	        }
	    }
	    GenericCore.Instance.netObjects.Clear();
	}

	private async Task WaitForXFrames(int x)
	{
		for(int i = 0; i < x; i++)
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}
}
