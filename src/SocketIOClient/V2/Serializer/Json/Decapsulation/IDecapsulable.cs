namespace SocketIOClient.V2.Serializer.Json.Decapsulation;

public interface IDecapsulable
{
    DecapsulationResult Decapsulate(string text);
}