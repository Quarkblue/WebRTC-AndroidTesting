using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class SimpleDataChannelReceiver : MonoBehaviour
{
    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;
    private string clientID;

    private bool hasReceivedOffer = false;
    private SessionDescription receivedOfferSessionDescTemp;

    public string serverIP;

    private void OnEnable()
    {
        EventManager.onDataChannelInitializeReceiver += Initialize;
    }

    private void Start()
    {
        InitClient(serverIP, 8080);
    }

    private void Update()
    {
        if (hasReceivedOffer)
        {
            hasReceivedOffer = !hasReceivedOffer;
            StartCoroutine(CreateAnswer());
        }
    }

    private void OnDisable()
    {
        EventManager.onDataChannelInitializeReceiver -= Initialize;
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    private void Initialize(string serverIp)
    {
        Debug.Log("Initializing server receiver");

        InitClient(serverIp, 8080);
    }

    public void InitClient(string serverIP, int serverPort)
    {
        Debug.Log("Initializing client");
        int port = serverPort == 0 ? 8080 : serverPort;
        clientID = gameObject.name;

        ws = new WebSocket($"ws://{serverIP}:{port}/{nameof(SimpleDataChannelService)}");

        ws.OnMessage += (sender, e) =>
        {
            var requestArray = e.Data.Split("!");
            var requestType = requestArray[0];
            var requestData = requestArray[1];

            switch (requestType)
            {
                case "OFFER":
                    Debug.Log(clientID + " - Got Offer from maximus: " + requestData);
                    receivedOfferSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedOffer = true;
                    break;
                case "CANDIDATE":
                    Debug.Log(clientID + " - Got CANDIDATE from Maximus: " + requestData);

                    var candidateInit = CandidateInit.FromJSON(requestData);
                    RTCIceCandidateInit init = new RTCIceCandidateInit();
                    init.sdpMid = candidateInit.SdpMid;
                    init.sdpMLineIndex = candidateInit.SdpMLineIndex;
                    init.candidate = candidateInit.Candidate;
                    RTCIceCandidate candidate = new RTCIceCandidate(init);

                    connection.AddIceCandidate(candidate);
                    break;
                default:
                    Debug.Log(clientID + " - Maximus says:" + e.Data);
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
        connection.OnDataChannel = channel =>
        {
            dataChannel = channel;
            dataChannel.OnMessage = bytes =>
            {
                var message = System.Text.Encoding.UTF8.GetString(bytes);
                Debug.Log("Receiver received: " + message);
            };
        };
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
