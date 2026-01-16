using SocketIOClient.Core;
using SocketIOClient.Observers;

namespace SocketIOClient.Protocol;

public interface IProtocolAdapter : IMyObservable<ProtocolMessage>, IMyObserver<ProtocolMessage>
{
    void SetDefaultHeader(string name, string value);
}