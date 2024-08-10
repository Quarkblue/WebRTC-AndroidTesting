using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GameManager : MonoBehaviour
{

    public string serverIp;

    public TextMeshProUGUI serverTextip;

    public GameObject senderObject, receiverObject;


    public void OnServerInitialized()
    {
        serverIp = serverTextip.text;
        if (senderObject.activeSelf && receiverObject.activeSelf)
        {
            Debug.Log("Sender and receiver BothInitialized");
            Debug.Log("ServerIP set to: " + serverIp);
            EventManager.DataChannelInitializeSender(serverIp);
            EventManager.DataChannelInitializeReceiver(serverIp);
        }
        else if(senderObject.activeSelf)
        {
            Debug.Log("Sender Initialized");
            Debug.Log("ServerIP set to: " + serverIp);
            EventManager.DataChannelInitializeSender(serverIp);
        }
        else if (receiverObject.activeSelf)
        {
            Debug.Log("Receiver Initialized");
            Debug.Log("ServerIP set to: " + serverIp);
            EventManager.DataChannelInitializeReceiver(serverIp);
        }
    }
}
