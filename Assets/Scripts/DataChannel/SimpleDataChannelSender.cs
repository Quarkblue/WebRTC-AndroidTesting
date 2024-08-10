using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using WebSocketSharp;

public class SimpleDataChannelSender : MonoBehaviour
{
    [SerializeField] private bool sendMessageViaChannel = false;

    private RTCPeerConnection connection;
    private RTCDataChannel dataChannel;

    private WebSocket ws;
    private string clientID;

    private bool hasReceivedAnswer = false;
    private SessionDescription receivedAnswerSessionDescTemp;

    public string serverIP;


    private void OnEnable()
    {
        EventManager.onDataChannelInitializeSender += Initialize;
    }

    private void Start()
    {
        InitClient(serverIP, 8080);
    }

    private void Update()
    {
        if (hasReceivedAnswer)
        {
            hasReceivedAnswer = !hasReceivedAnswer;
            StartCoroutine(SetRemoteDesc());
        }
        if (sendMessageViaChannel)
        {
            sendMessageViaChannel = !sendMessageViaChannel;
            dataChannel.Send("TEST! TEST! TEST!");
        }
    }

    private void OnDisable()
    {
        EventManager.onDataChannelInitializeSender -= Initialize;
    }

    private void OnDestroy()
    {
        dataChannel.Close();
        connection.Close();
    }

    public void Initialize(string serverIp)
    {
        Debug.Log("Initializing server sender");

        InitClient(serverIp, 8080);
    }

    private void InitClient(string serverIp, int serverPort)
    {
        int port = serverPort == 0 ? 8080 : serverPort;
        clientID = gameObject.name;

        ws = new WebSocket($"ws://{serverIp}:{port}/{nameof(SimpleDataChannelService)}");

        ws.OnMessage += (sender, e) => {
            var requestArray = e.Data.Split("!");
            var requestType = requestArray[0];
            var requestData = requestArray[1];

            switch(requestType)
            {
                case "ANSWER":
                    Debug.Log(clientID + "- Got ANSWER from Maximus: " + requestData);
                    receivedAnswerSessionDescTemp = SessionDescription.FromJSON(requestData);
                    hasReceivedAnswer = true;
                    break;
                case "CANDIDATE":
                    Debug.Log(clientID + " - Got CANDIDATE from Maximux: " + requestData);
                    var candidateInit = CandidateInit.FromJSON(requestData);

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

        dataChannel = connection.CreateDataChannel("sendChannel");
        dataChannel.OnOpen = () =>
        {
            Debug.Log("Sender opened channel");
        };

        dataChannel.OnClose = () =>
        {
            Debug.Log("Sender closed channel");
        };

        connection.OnNegotiationNeeded = () =>
        {
            StartCoroutine(CreateOffer());
        };
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
