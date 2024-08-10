using System;

public class SignalingMessage
{
    public readonly SignalingMessageTypes Type;
    public readonly string Message;

    public SignalingMessage(string messageString)
    {
        var messageArray = messageString.Split("!");
        if (messageArray.Length < 2)
        {
            Type = SignalingMessageTypes.OTHER;
            Message = messageString;
        }
        else if (Enum.TryParse(messageArray[0], out SignalingMessageTypes resultType))
        {
            Type = resultType;
            Message = messageArray[1];
        }
    }

}
