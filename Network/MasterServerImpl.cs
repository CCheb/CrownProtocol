using Godot;
using System;

public partial class MasterServerImpl : INetworkRole
{
    private MasterNetworkManager masterInstance;
    private const int MASTER = 1;
    public MasterServerImpl(MasterNetworkManager manager)
    {
        masterInstance = manager;
    }

    public void StartUp()
    {
        masterInstance.CreateMasterServer();
    }

    public void OnPeerConnected(long id)
    {
        return;
    }
    public void OnPeerDisconnected(long id)
    {
        GD.Print($"Master Network - Peer: {masterInstance.Multiplayer.GetUniqueId()} recieved disconnection from peer {id}");

        if(masterInstance.availableServers.ContainsKey(id))
        {
            masterInstance.availableServers.Remove(id);
            GD.Print($"Master Network - Server {id} removed. Available servers now: {masterInstance.availableServers}");
        }
    }
}
