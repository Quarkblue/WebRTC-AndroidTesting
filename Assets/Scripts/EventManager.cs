using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public delegate void OnDataChannelInitializeReceiver(string serverIp);
    public static event OnDataChannelInitializeReceiver onDataChannelInitializeReceiver;


    public delegate void OnDataChannelInitializeSender(string serverIp);
    public static event OnDataChannelInitializeSender onDataChannelInitializeSender;


    public static void DataChannelInitializeReceiver(string serverIp)
    {
        if (onDataChannelInitializeReceiver != null)
        {
            onDataChannelInitializeReceiver(serverIp);
        }
    }

    public static void DataChannelInitializeSender(string serverIp)
    {
        if (onDataChannelInitializeSender != null)
        {
            onDataChannelInitializeSender(serverIp);
        }
    }
}
