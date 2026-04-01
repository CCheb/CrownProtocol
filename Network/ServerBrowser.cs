using Godot;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

public partial class ServerBrowser : Control
{
	[Export] private GridContainer gameServerContainer;
	[Export] private PackedScene serverJoinButton;
	[Export] private LineEdit userNameEntryBox;
	[Export] private WarningScreen warningScreen;
	private const int MASTER = 1;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		MasterNetworkManager.Instance.AvailableServersChanged += OnGameServersChanged;

		MasterNetworkManager.Instance.GameServerIsBusy += ShowWarningScreen;
		MasterNetworkManager.Instance.GameServerIsAvailable += ClientWantsToJoin;
	}

    public override void _ExitTree()
    {
        base._ExitTree();
		MasterNetworkManager.Instance.AvailableServersChanged -= OnGameServersChanged;
		MasterNetworkManager.Instance.GameServerIsBusy -= ShowWarningScreen;
		MasterNetworkManager.Instance.GameServerIsAvailable -= ClientWantsToJoin;
		MasterNetworkManager.Instance.availableServers = []; // Clear local cache before moving on
    }

	[Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void RequestForNewGameServers()
	{	
		// Client sends over its copy of the available servers. Master server compares and updates new copy if needed
		MasterNetworkManager.Instance.RpcId(MASTER, "QueryForNewGameServers", MasterNetworkManager.Instance.availableServers);
	}

	private async void OnGameServersChanged()
	{
		ClearAllButtons();
		await AddNewButtons();
	}

	private void ClearAllButtons()
	{
		foreach(ServerJoinButton b in gameServerContainer.GetChildren())
		{
			b.QueueFree();
		}
	} 

	private async Task AddNewButtons()
	{
		foreach(var serverEntry in MasterNetworkManager.Instance.availableServers)
		{
			ServerJoinButton newButton = serverJoinButton.Instantiate<ServerJoinButton>();
			gameServerContainer.AddChild(newButton);
			newButton.ClientClickedJoinButton += OnClientClickedJoin;
			
			newButton.AssociateButtonWithServer(serverEntry.Key, serverEntry.Value["ServerName"]);
			await ToSignal(GetTree().CreateTimer(0.1f), "timeout");
		}
	}

	private void OnClientClickedJoin(long serverId)
	{
		// Must check if the server is busy here. Need to talk to Master server to get the latest server status
		MasterNetworkManager.Instance.RpcId(MASTER, "QueryGameServerStatus", serverId);

		/*
		// User serverName to query availableServers
		Godot.Collections.Dictionary<string, string> serverEntry = MasterNetworkManager.Instance.availableServers[serverId];

		// Will get picked up by local GenericCore
		MasterNetworkManager.Instance.EmitSignal("ClientWantsToJoin", userNameEntryBox.Text, serverEntry["Port"]);
		*/
	}

	private void ShowWarningScreen()
	{
		warningScreen.Visible = true;
	}

	private void ClientWantsToJoin(long serverId)
	{
		// User serverName to query availableServers
		Godot.Collections.Dictionary<string, string> serverEntry = MasterNetworkManager.Instance.availableServers[serverId];

		// Will get picked up by local GenericCore
		MasterNetworkManager.Instance.EmitSignal("ClientWantsToJoin", userNameEntryBox.Text, serverEntry["Port"]);
	}

	
}
