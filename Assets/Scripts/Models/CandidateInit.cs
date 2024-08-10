using System;
using UnityEngine;

public class CandidateInit : IJsonObject<CandidateInit>
{
    public string Candidate;
    public string SdpMid;
    public int SdpMLineIndex;

    public static CandidateInit FromJSON(string jsonString)
    {
        return JsonUtility.FromJson<CandidateInit>(jsonString);
    }

    public string ConverToJSON()
    {
        return JsonUtility.ToJson(this);
    }


}
