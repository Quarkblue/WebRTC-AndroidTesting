using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using WebSocketSharp;

public class SimpleMediaStreamSender : MonoBehaviour
{
    [SerializeField] private Camera cameraStream;
    [SerializeField] private RawImage sourceImage;

    private RTCPeerConnection connection;
    private MediaStream videoStream;

    private VideoStreamTrack VideoStreamTrack;

    private WebSocket ws;

    private string clientID;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;


    //public string serverIP;

    private void Start()
    {
        StartCoroutine(wait(2));
        InitClient("192.168.1.5", 8080);
    }

    private IEnumerator wait(float time)
    {
        yield return new WaitForSeconds(time);
    }

    private void Update()
    {
        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }
    }

    public void InitClient(string serverIP, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        clientID = gameObject.name;

        ws = new WebSocket($"ws://{serverIP}:{port}/{nameof(SimpleDataChannelService)}");
        ws.OnMessage += (sender, e) =>
        {
            var singalingMessage = new SignalingMessage(e.Data);

            switch (singalingMessage.Type)
            {
                case SignalingMessageTypes.ANSWER:
                    Debug.Log($"{clientID} - Got ANSWER from Maximus: {singalingMessage.Message}");
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(singalingMessage.Message);
                    hasReceivedAnswer = true;
                    break;

                case SignalingMessageTypes.CANDIDATE:
                    Debug.Log($"{clientID} - Got CANDIDATE from Maximus: {singalingMessage.Message}");

                    var candidateInit = CandidateInit.FromJSON(singalingMessage.Message);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    RTCIceCandidate candidate = new RTCIceCandidate(init);

                    connection.AddIceCandidate(candidate);
                    break;

                default:
                    Debug.Log(clientID + " - Maximus Says: " + e.Data);
                    break;
            }
        };

        ws.Connect();

        connection = new RTCPeerConnection();
        connection.OnIceCandidate = candidate =>
        {
            var candidateInit = new CandidateInit()
            {
                SdpMid = candidate.SdpMid,
                SdpMLineIndex = candidate.SdpMLineIndex ?? 0,
                Candidate = candidate.Candidate
            };
            ws.Send("CANDIDATE!" + candidateInit.ConverToJSON());
        };
        connection.OnIceConnectionChange = state =>
        {
            Debug.Log(state);
        };


        connection.OnNegotiationNeeded = () =>
        {
            StartCoroutine(CreateOffer());
        };


        VideoStreamTrack = cameraStream.CaptureStreamTrack(1280, 720);
        sourceImage.texture = cameraStream.targetTexture;
        connection.AddTrack(VideoStreamTrack);

        StartCoroutine(WebRTC.Update());

    }

    private void OnDestroy()
    {
        VideoStreamTrack.Stop();
        connection.Close();

        ws.Close();
    }

    private IEnumerator CreateOffer()
    {
        var offer = connection.CreateOffer();
        yield return offer;

        var offerDesc = offer.Desc;
        var localDescOp = connection.SetLocalDescription(ref offerDesc);
        yield return localDescOp;

        var offerSessionDesc = new SessionDescription()
        {
            SessionType = offerDesc.type.ToString(),
            Sdp = offerDesc.sdp
        };

        ws.Send("OFFER!" + offerSessionDesc.ConverToJSON());
    }

    private IEnumerator SetRemoteDesc()
    {
        RTCSessionDescription answerSessionDesc = new RTCSessionDescription();
        answerSessionDesc.type = RTCSdpType.Answer;
        answerSessionDesc.sdp = receivedAnswerSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref answerSessionDesc);
        yield return remoteDescOp;
    }


}
