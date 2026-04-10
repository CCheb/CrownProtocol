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

		if (netCore == null)
			GD.Print("netCore is null");

		netCore.PlayerJoined += OnPlayerJoined;
    }


	private void OnQuitButtonPressed()
	{
		beep.Play();
		GenericCore.Instance.DisconnectFromGame();
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
