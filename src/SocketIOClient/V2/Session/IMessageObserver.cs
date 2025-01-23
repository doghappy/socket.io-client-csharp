using SocketIOClient.V2.Message;

namespace SocketIOClient.V2.Session;

public interface IMessageObserver
{
    void OnNext(IMessage message);
}