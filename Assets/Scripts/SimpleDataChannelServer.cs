using System.Net.Sockets;
using System.Net;
using UnityEngine;
using WebSocketSharp.Server;
using System;

public class SimpleDataChannelServer : MonoBehaviour
{

    private WebSocketServer wssv;
    private string serverIpv4Address;
    private int serverPort = 8080;

    void Awake()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                serverIpv4Address = ip.ToString();
                Debug.Log(serverIpv4Address);
                break;
            }
        }

        wssv = new WebSocketServer($"ws://{serverIpv4Address}:{serverPort}");
        wssv.AddWebSocketService<SimpleDataChannelService>($"/{nameof(SimpleDataChannelService)}");
        wssv.Start();

    }

    private void OnDestroy()
    {
        wssv.Stop();
    }
}
