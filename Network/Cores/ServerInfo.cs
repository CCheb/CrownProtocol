using Godot;
using System;

public partial class ServerInfo : Node
{
    public string gameServerName;
    public string portNumber;

    public ServerInfo(string gameServerName, string portNumber)
    {
        this.gameServerName = this.portNumber;
    }
}
