using Godot;
using System;

public partial class MasterGameServerImpl : INetworkRole
{
    private MasterNetworkManager masterInstance;
    private const int MASTER = 1;
    public MasterGameServerImpl(MasterNetworkManager manager)
    {
        masterInstance = manager;
    }

    public void StartUp()
    {
        masterInstance.CheckIPAddresses();
        masterInstance.JoinMasterServer();
    }

    public void OnPeerConnected(long id)
    {
        GD.Print("Master Network - Connected to Master Server\n");

        if(id == MASTER)
        {
            masterInstance.RpcId(id, "RegisterNewGameServer", masterInstance.tempGameName, masterInstance.tempPortNumber);
            masterInstance.EmitSignal("ServerIsConnected");
        }
        
    }
    public void OnPeerDisconnected(long id)
    {
        GD.Print($"Master Network - Peer: {masterInstance.Multiplayer.GetUniqueId()} recieved disconnection from peer {id}");
    }
}
