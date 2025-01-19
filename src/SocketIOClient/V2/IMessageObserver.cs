using SocketIOClient.V2.Message;

namespace SocketIOClient.V2;

public interface IMessageObserver
{
    void OnNext(IMessage message);
}