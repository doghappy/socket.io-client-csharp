namespace SocketIOClient.V2.Serializer.Json.Decapsulation;

public interface IDecapsulable
{
    DecapsulationResult DecapsulateRawText(string text);
    MessageResult DecapsulateEventMessage(string text);
    BinaryEventMessageResult DecapsulateBinaryEventMessage(string text);
}