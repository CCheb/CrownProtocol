using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class GenericCore : Node
{
    [Signal]
    public delegate void ClientConnectedNotifierEventHandler(long peerId, Dictionary<string, string> peerInfo);
    [Signal]
    public delegate void ClientDisconnectedNotifierEventHandler(long peerId);
    [Signal]
    public delegate void ClientServerNotFoundNotifierEventHandler(Error error);
    [Signal]
    public delegate void ServerCreatedEventHandler(Dictionary<string, string> serverInfo);
    [Signal]
    public delegate void ServerDisconnectedEventHandler();
    [Signal]
    public delegate void ServerFailedEventHandler(Error error);
    [Signal]
    public delegate void ConnectedPeersDictionaryUpdatedEventHandler(long newPeerId);

    private int localPort = 9000;
    private int portMinimum = 9000;
    private int portMaximum = 9100;

    private string serverAddress = "127.0.0.1"; 
    private int maxClientConnections = 4;

    public Dictionary<long, Dictionary<string, string>> connectedPeers = new();
    private Dictionary<string, string> localPeerInfo = new()
    {
        { "NetID", "1" },
        { "UserName", "John Doe"}
    };

    public Dictionary<int, NetID> netObjects = new();  // Server will be only one to see netObjects
    private Array<Node> nodesForErase = new Array<Node>();

    public static GenericCore Instance { get; private set; }

    public bool IsServer;
    public bool PeerConnected;
    public bool IsGenericCoreConnected;

   
    public override void _Ready()
    {
        base._Ready();
        // Instance at this point will always be initialized
        MasterNetworkManager.Instance.ServerIsConnected += InitializeServer;
        MasterNetworkManager.Instance.ClientWantsToJoin += InitializeClient;
    }

    // This runs when the server connects to the master server
    private void InitializeServer()
    {
        SetInstance();
        SetNetworkSignals();
        CheckForCommandLineArgs();
    }

    // This runs when the client clicks one of the join buttons
    private void InitializeClient(string userName, string portNumber)
    {
        SetInstance();
        SetNetworkSignals();
        SetPort(portNumber);
        ParseInitialPromptInfo(userName);

        if(JoinGame() != Error.Ok)
        {
            GD.PrintErr("Could not connect to game server! Server might not exists or is busy\n");
            DisconnectNetworkSignals();
        }
    }

    private void SetInstance()
    {
        Instance ??= this;
        GD.Print("Generic Core - Instance static variable set!"); 
    }

    private void SetNetworkSignals()
    {
        Multiplayer.PeerConnected += OnPeerConnected;
        Multiplayer.PeerDisconnected += OnPeerDisconnected;
        Multiplayer.ConnectedToServer += OnClientConnectSuccess;
        Multiplayer.ConnectionFailed += OnConnectionToServerFail;
        Multiplayer.ServerDisconnected += OnServerDisconnected;
    }

    private void DisconnectNetworkSignals()
    {
        Multiplayer.PeerConnected -= OnPeerConnected;
        Multiplayer.PeerDisconnected -= OnPeerDisconnected;
        Multiplayer.ConnectedToServer -= OnClientConnectSuccess;
        Multiplayer.ConnectionFailed -= OnConnectionToServerFail;
        Multiplayer.ServerDisconnected -= OnServerDisconnected;
    }

    private void CheckForCommandLineArgs()
    {
        string[] args = OS.GetCmdlineArgs();
        for(int i = 0; i < args.Count(); i++)
        {
            if (args[i] == "GAMESERVER")
            {
                SetPort(args[i + 1]);
                CreateGame();
                break;
            }
        }
    }

    //TODO: MORE ROBUST SETTER HERE
    public void SetPort(string s)
    {   
        localPort = int.Parse(s);
    }

    //TODO: MORE MEANINGFUL GETTER HERE 
    public int GetPort()
    {
        return localPort;
    }

    public void ParseInitialPromptInfo(string userName)
    {
        localPeerInfo["UserName"] = userName;
    }

    // Client starts here. This gets called when a client peer clicks the join game button
    public Error JoinGame()
    {
        GD.Print($"Generic Core - Attempting to connect to {serverAddress}:{localPort}");
        
        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateClient(serverAddress, localPort);
        if (error != Error.Ok) 
            return error;

        GD.Print("Generic Core - Connected to server\n");

        
        // PeerConnected and ConnectedToServer signals will implicitly trigger if connection is made to server
        Multiplayer.MultiplayerPeer = peer;

        IsGenericCoreConnected = true;
        PeerConnected = true;


        GetTree().ChangeSceneToFile("res://Scenes/gameLobby.tscn"); // Could be risky switching scenes here
        return Error.Ok;
    }

    // Server starts here. This gets called when a server peer clicks the host game button
    public Error CreateGame()
    {
        GD.Print($"Generic Core - Attempting to create server at {serverAddress}:{localPort}");

        var peer = new ENetMultiplayerPeer();
        Error error = peer.CreateServer(localPort, maxClientConnections);
        if (error != Error.Ok)
        {
            EmitSignalServerFailed(error);
            return error;
        }

        GD.Print("Generic Core - Created Local Game\n");

        // If server, no signals get emitted since its techinically the first peer
        // Because of this we need to set things up manually by setting the _localPeerInfo
        Multiplayer.MultiplayerPeer = peer;

        connectedPeers[1] = localPeerInfo;  // Defaults to John Doe
        
        IsServer = true;
        IsGenericCoreConnected = true;
        PeerConnected = true;

        // In the case that the server was a playable character
        EmitSignalServerCreated(localPeerInfo);

        

        // Should be an independent run. Will not influence Master or GenericCore
        GetTree().ChangeSceneToFile("res://Scenes/gameLobby.tscn");
        //EmitSignalServerIsGood();
        return Error.Ok;
    }

    // Emits on local client that just connected. Emits before OnPeerConnected
    private void OnClientConnectSuccess()
    {
        int peerId = Multiplayer.GetUniqueId();
        connectedPeers[peerId] = localPeerInfo; 
    }

    // Sends a message to the rest of the clients to register this peer
    private void OnPeerConnected(long id)
    {   
        // Plays both ways. When client connects it sends its info to other connected peers (via id)
        // Other connected peers then send their info back to the player (via id)
        RpcId(id, MethodName.RegisterPeer, localPeerInfo);
    
        GD.Print("Generic Core - Client Connected!");
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void RegisterPeer(Dictionary<string, string> peerInfo)
    {
        // From the perspective of the receiver. Goes both ways
        int newPeerId = Multiplayer.GetRemoteSenderId(); //Who is sending to call this function

        GD.Print($"Generic Core - Peer {Multiplayer.GetUniqueId()} registering peer {newPeerId}\n");

        if (newPeerId == 1 && !Multiplayer.IsServer())  
            peerInfo["NetID"] = Multiplayer.GetUniqueId().ToString();
        else
            peerInfo["NetID"] = newPeerId.ToString(); 

        //Updating the dictionary for the new player
        connectedPeers[newPeerId] = peerInfo;
        EmitSignalClientConnectedNotifier(newPeerId, peerInfo); // Notify NetCore for object creation
        EmitSignalConnectedPeersDictionaryUpdated(newPeerId);   // Not everyone subscribes to these signals

        GD.Print(connectedPeers);
    }

    public void RegisterObject(NetID netId)
    {
        // Server is the peer that can see all the valid netObjects
        netId.netObjectID = (uint)Instance.netObjects.Count;
        Instance.netObjects.Add(Instance.netObjects.Count, netId);
    }
    
    // Sends a message to all connectedPeers that a client disconnected
    // Also removes player from connected peers table
    private void OnPeerDisconnected(long id)
    {
        GD.Print($"GenericCore - Peer {Multiplayer.GetUniqueId()} got disconnection signal from {connectedPeers[id]["UserName"]}\n");
        connectedPeers.Remove(id);
        //Need to destroy objects.
        EmitSignalClientDisconnectedNotifier(id);
    }

    private void OnConnectionToServerFail()
    {
        Multiplayer.MultiplayerPeer = null;
    }

    private void OnServerDisconnected()
    {
        // null == complete network disconnecion/reset
        Multiplayer.MultiplayerPeer = null;
        connectedPeers.Clear();
        EmitSignalServerDisconnected();
    }

    public void DisconnectFromGame()
    {   
        // Client disconnection
        if (Multiplayer.MultiplayerPeer != null)
        {
            GD.Print($"Generic Core - {connectedPeers[Multiplayer.GetUniqueId()]["UserName"]} Disconnecting from ENet session...");

            DisconnectNetworkSignals();
            // Close the connection
            Multiplayer.MultiplayerPeer.Close();    // This will emitt OnPeerDisconnected on all remaining peers

            // Remove the peer from the Multiplayer API
            Multiplayer.MultiplayerPeer = null;
            connectedPeers.Clear();
            netObjects.Clear();
            //_netObjectsCount = 0;

            IsGenericCoreConnected = false;
            IsServer = false;

            GD.Print("Returning back to server browser\n");
            GetTree().ChangeSceneToFile("res://UI/Scenes/start_screen.tscn"); // Could emit a signal here to have the game manager switch for me
        }
    }

    

    //----//

    public int playersLoaded = 0;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer,CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void PlayerLoaded()
    {
        if (Multiplayer.IsServer())
        {
            GD.Print($"Generic Core - Peer {Multiplayer.GetRemoteSenderId()} just loaded in!");
            playersLoaded += 1;
            if (playersLoaded == Instance.connectedPeers.Count)
            {
                playersLoaded = 0;
                Rpc("StartGame");
            }
        }
    }

    [Rpc(CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
	private void StartGame()
	{
		GD.Print($"Generic Core - Peer {Multiplayer.GetUniqueId()} now starting...\n");
		PackedScene level = (PackedScene)ResourceLoader.LoadThreadedGet("res://Scenes/level.tscn");
		GetTree().ChangeSceneToPacked(level);
    }

    private int playersSceneReady = 0;

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SceneReady()
    {
        if (!Multiplayer.IsServer())
            return;

        playersSceneReady++;

        GD.Print($"SceneReady: {playersSceneReady}/{connectedPeers.Count}");

        if (playersSceneReady == connectedPeers.Count)
        {
            playersSceneReady = 0;
            // Once everyone has properly entered the scene tree then we signal to start spawning
            Rpc(MethodName.BeginSpawning);
        }
    }

    [Rpc(CallLocal = true,TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void BeginSpawning()
    {
        GD.Print($"Peer {Multiplayer.GetUniqueId()} BEGIN SPAWNING");

        if (IsServer)
        {
            var gm = GetTree().Root.GetNode<GameManager>("GameManager");
            gm.ServerSpawnItems();
            gm.ServerSpawnPlayers();
        }
    }
}