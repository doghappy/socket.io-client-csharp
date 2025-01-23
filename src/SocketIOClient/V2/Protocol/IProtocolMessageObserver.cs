namespace SocketIOClient.V2.Protocol;

public interface IProtocolMessageObserver
{
    void OnNext(ProtocolMessage message);
}