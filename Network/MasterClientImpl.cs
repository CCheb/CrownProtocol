using Godot;
using System;

public partial class MasterClientImpl : INetworkRole
{
    private MasterNetworkManager masterInstance;
    private const int MASTER = 1;
    public MasterClientImpl(MasterNetworkManager manager)
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
            masterInstance.RpcId(id, "QueryForNewGameServers", masterInstance.availableServers);
        }
        
    }
    public void OnPeerDisconnected(long id)
    {
        GD.Print($"Master Network - Peer: {masterInstance.Multiplayer.GetUniqueId()} recieved disconnection from peer {id}");
    }
}

