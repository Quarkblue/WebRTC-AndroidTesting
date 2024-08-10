using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionDescription : IJsonObject<SessionDescription>
{
    public string SessionType;
    public string Sdp;

    public string ConverToJSON()
    {
        return JsonUtility.ToJson(this);
    }

    public static SessionDescription FromJSON(string jsonString)
    {
        return JsonUtility.FromJson<SessionDescription>(jsonString);
    }
    
}
