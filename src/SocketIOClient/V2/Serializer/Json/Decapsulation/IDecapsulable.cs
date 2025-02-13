namespace SocketIOClient.V2.Serializer.Json.Decapsulation;

public interface IDecapsulable
{
    DecapsulationResult Decapsulate(string text);
    EventMessageResult DecapsulateEventMessage(string text);
    BinaryEventMessageResult DecapsulateBinaryEventMessage(string text);
}