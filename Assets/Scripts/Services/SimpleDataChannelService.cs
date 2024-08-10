using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

public class SimpleDataChannelService : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log("SERVER SimpleDataChannelServive started");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log(ID + " - DataChannel SERVER got message" + e.Data);

        //forward the message to all connected clients except the sender of the message
        foreach(var id in Sessions.ActiveIDs)
        {
            if(id != ID)
            {
                Sessions.SendTo(e.Data, id);
            }
        }
    }
}
