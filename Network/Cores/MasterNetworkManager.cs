using Godot;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

public partial class MasterNetworkManager : Node
{
    [Signal] public delegate void AvailableServersChangedEventHandler();
    [Signal] public delegate void ServerIsConnectedEventHandler();  // For when game server connects
    [Signal] public delegate void ClientWantsToJoinEventHandler(string userName, string portNumber);    // For when player client connects

    [Signal] public delegate void GameServerIsBusyEventHandler();
    [Signal] public delegate void GameServerIsAvailableEventHandler(long id);
    public Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>> availableServers = new();
    private MultiplayerApi AgentAPI;
    public  ENetMultiplayerPeer AgentPeer;
    public static MasterNetworkManager Instance;

    public int PortMinimum = 9000;
    public string PublicIP = "127.0.0.1";
    public string PrivateIP = "127.0.0.1";
    private string[] ipAddresses;
    private NetworkPing np;

    public string LobbyServerIP;
    public bool IsMasterServer;   // Used by MASTER  

    public string tempGameName;
    public string tempPortNumber;

    private INetworkRole role = null;

    public override void _Ready()
    {
        base._Ready();

        Instance ??= this;

        ipAddresses =
        [
            PublicIP,
            PrivateIP,
            "127.0.0.1" // Local Host will always work when checked
        ];

        np = new();

        CreateCustomMultiplayerAPI();

        CheckForCommandLineArgs();

        role.StartUp();
    }

    private void CreateCustomMultiplayerAPI()
    {
        AgentAPI = MultiplayerApi.CreateDefaultInterface();
        AgentAPI.PeerConnected += OnPeerConnected;
        AgentAPI.PeerDisconnected += OnPeerDisconnected;

        // Switching to custom multiplayer API
        GetTree().SetMultiplayer(AgentAPI, GetPath()); 
    }

    private void CheckForCommandLineArgs()
    {
        string[] args = OS.GetCmdlineArgs();

        bool foundRole = false;
        
        for(int i = 0; i < args.Count(); i++)  
        {
            if (args[i] == "MASTER")
            {
                role = new MasterServerImpl(Instance);
                foundRole = true;
            }
            if(args[i] == "GAMESERVER")
            { 
                // Changing here causes game server to act as client?
                if (args.Length < 3)
                {
                    GD.PrintErr("Not enough arguments for GAMESERVER");
                    return;
                }

                tempGameName = args[2].Split("#")[1];   
                tempPortNumber = args[1]; 

                role = new MasterGameServerImpl(Instance);

                foundRole = true;

                break;
            }
        }

        if(!foundRole)
        {
            // Default to client
            role = new MasterClientImpl(Instance);
        }
    }

    public Error CreateMasterServer()
    {
        GD.Print("Master Network - Attempting to create lobby system at port: " + PortMinimum);
        AgentPeer = new ENetMultiplayerPeer();

        Error err = AgentPeer.CreateServer(PortMinimum, 1000); // default port 9000
        AgentAPI.MultiplayerPeer = AgentPeer; // Since its server nothing gets emitted here
        if (err != Error.Ok)
        {
            GD.Print(err.ToString());
            return err;
        }

        GD.Print("Master Network - Master Server Created!\n");
        IsMasterServer = true;
        
        return Error.Ok;
    }

    public void CheckIPAddresses()
    {
        for(int i = 0; i < ipAddresses.Length; i++)
        {
            if(np.Check(ipAddresses[i]) == System.Net.NetworkInformation.IPStatus.Success)
            {
                LobbyServerIP = ipAddresses[i];
                break;
            }
        }
    }

    public Error JoinMasterServer()
    {   
        GD.Print($"Master Network - Attempting to connect to {LobbyServerIP}:{PortMinimum}");
        AgentPeer = new ENetMultiplayerPeer();

        Error error = AgentPeer.CreateClient(LobbyServerIP, PortMinimum);
        AgentAPI.MultiplayerPeer = AgentPeer; // OnPeerConnected emits here
        if (error != Error.Ok)
            return error;

        return Error.Ok;
    }

    private void OnPeerConnected(long id)
    {
        role.OnPeerConnected(id);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterNewGameServer(string serverName, string port)
    {
        if(!IsMasterServer)
            return;
        
        GD.Print("Master Network - Recieved Register signal!");

        Godot.Collections.Dictionary<string, string> serverEntry = new()
        {
            { "ServerName", serverName },
            { "Port", port },
            { "Status", "INLOBBY"}
        };

        availableServers[Multiplayer.GetRemoteSenderId()] = serverEntry;

        GD.Print($"Master Network - Available servers now: {availableServers}\n");
        
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void QueryForNewGameServers(Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>> oldGameServer)
    {
        if(!IsMasterServer)
            return;
    
        GD.Print("Master Network - Received Query signal");
        if(oldGameServer.RecursiveEqual(availableServers))
        {
            GD.Print("Master Network - No new servers");
        }
        else
        {
            RpcId(Multiplayer.GetRemoteSenderId(), MethodName.SendAvailableGameServers, availableServers);
        }

    }

    [Rpc(MultiplayerApi.RpcMode.Authority, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SendAvailableGameServers(Godot.Collections.Dictionary<long, Godot.Collections.Dictionary<string, string>> availableServers)
    {
        // Guaranteed to have new game server listings
        this.availableServers = [];

        foreach (var server in availableServers)
        {
            this.availableServers[server.Key] = new Godot.Collections.Dictionary<string, string>(server.Value);
        }

        GD.Print($"Master Network - Client can see these servers {this.availableServers}");
        EmitSignalAvailableServersChanged();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ChangeGameServerStatus(string status)
    {
        if(!IsMasterServer)
            return;
        
        GD.Print($"Master Network - Changed game {Multiplayer.GetRemoteSenderId()} status to {status}");
        availableServers[Multiplayer.GetRemoteSenderId()]["Status"] = status;
    }


    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void QueryGameServerStatus(long id)
    {
        RpcId(Multiplayer.GetRemoteSenderId(), MethodName.SendGameServerStatus, id, availableServers[id]["Status"]);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SendGameServerStatus(long id, string status)
    {
        // From client perspective
        availableServers[id]["Status"] = status;
        TryToJoin(id);
    }

    private void TryToJoin(long gameServerId)
    {
        if(availableServers[gameServerId]["Status"] == "INLOBBY")
            EmitSignalGameServerIsAvailable(gameServerId);
        else
            EmitSignalGameServerIsBusy();
        
    }


    private void OnPeerDisconnected(long id)
    {
        role.OnPeerDisconnected(id);
    }
}
