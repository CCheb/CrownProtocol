using Godot;
using System.Text;
using System;

public partial class NetworkPing : Node
{
    private System.Net.NetworkInformation.Ping ping;
    private System.Net.NetworkInformation.PingOptions pingOptions;
    private System.Net.NetworkInformation.PingReply pingReply;
    public NetworkPing()
    {
        ping = new System.Net.NetworkInformation.Ping();
        pingOptions = new System.Net.NetworkInformation.PingOptions();
        pingOptions.DontFragment = true;
    }

    private System.Net.NetworkInformation.PingReply SendPacketsTo(string IpAddress)
    {   
        // Create data packet
        string data = "HELLLLOOOOO!";
        byte[] buffer = ASCIIEncoding.ASCII.GetBytes(data);
        int timeout = 500;

        return ping.Send(IpAddress, timeout);
    }

    public System.Net.NetworkInformation.IPStatus Check(string ipAddress)
    {
        pingReply = SendPacketsTo(ipAddress);
        GD.Print("Master Network -  Ping Return: " + pingReply.Status.ToString());

        if(pingReply.Status == System.Net.NetworkInformation.IPStatus.Success)
            GD.Print("Master Network - The public IP responded with a roundtrip time of: " + pingReply.RoundtripTime);

        return pingReply.Status;
    }
}
