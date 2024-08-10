using System.Collections;
using System.Collections.Generic;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.UI;
using WebSocketSharp;

public class SimpleMediaStreamReceiver : MonoBehaviour
{
    [SerializeField] private RawImage receiveImage;

    private RTCPeerConnection connection;

    private WebSocket ws;
    private string clientID;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    private string senderIP;
    private int senderPort;

    public string serverIP;


    private void Start()
    {
        InitClient(serverIP, 8080);
    }

    public void InitClient(string serverIP, int serverPort)
    {
        senderPort = serverPort == 0 ? 8080 : serverPort;
        senderIP = serverIP;
        clientID = gameObject.name;

        ws = new WebSocket($"ws://{senderIP}:{senderPort}/{nameof(SimpleDataChannelService)}");

        ws.OnMessage += (sender, e) =>
        {
            var signalingMessage = new SignalingMessage(e.Data);

            switch (signalingMessage.Type)
            {
                case SignalingMessageTypes.OFFER:
                    Debug.Log($"{clientID} - Got OFFER from Maximus: {signalingMessage.Message}");
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(signalingMessage.Message);
                    hasReceivedOffer = true;
                    break;

                case SignalingMessageTypes.CANDIDATE:
                    Debug.Log($"{clientID} - Got CANDIDATE from Maximus: {signalingMessage.Message}");
                    var candidateInit = CandidateInit.FromJSON(signalingMessage.Message);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    RTCIceCandidate candidate = new RTCIceCandidate(init);

                    connection.AddIceCandidate(candidate);
                    break;

                default:
                    Debug.Log(clientID + " - Maximus says: " + e.Data);
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

        connection.OnTrack = e =>
        {
            if (e.Track is VideoStreamTrack video)
            {
                video.OnVideoReceived += tex =>
                {
                    receiveImage.texture = tex;
                };
            }
        };

        StartCoroutine(WebRTC.Update());
    }

    private void Update()
    {
        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
    }

    private void OnDestroy()
    {
        connection.Close();
        ws.Close();
    }

    private IEnumerator CreateAnswer()
    {
        RTCSessionDescription offerSessionDesc = new RTCSessionDescription();
        offerSessionDesc.type = RTCSdpType.Offer;
        offerSessionDesc.sdp = receivedOfferSessionDescTemp.Sdp;

        var remoteDescOp = connection.SetRemoteDescription(ref offerSessionDesc);
        yield return remoteDescOp;

        var answer = connection.CreateAnswer();
        yield return answer;

        var answerDesc = answer.Desc;
        var localDescOp = connection.SetLocalDescription(ref answerDesc);
        yield return localDescOp;

        var answerSessionDesc = new SessionDescription()
        {
            SessionType = answerDesc.type.ToString(),
            Sdp = answerDesc.sdp
        };
        ws.Send("ANSWER!" + answerSessionDesc.ConverToJSON());
    }

}
