namespace SocketIOClient.V2.Serializer.Decapsulation;

public interface IDecapsulable
{
    DecapsulationResult Decapsulate(string text);
}