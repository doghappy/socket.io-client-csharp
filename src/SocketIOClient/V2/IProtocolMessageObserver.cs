namespace SocketIOClient.V2;

public interface IProtocolMessageObserver
{
    void OnNext(ProtocolMessage message);
}