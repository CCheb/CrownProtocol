public partial interface INetworkRole
{
    public void StartUp();
    public void OnPeerConnected(long id);
    public void OnPeerDisconnected(long id);
}
