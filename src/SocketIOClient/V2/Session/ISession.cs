using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.V2.Message;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;

namespace SocketIOClient.V2.Session;

public interface ISession : IMyObserver<ProtocolMessage>, IMyObservable<IMessage>
{
    SessionOptions SessionOptions { get; set; }
    Task SendAsync(IMessage message, CancellationToken cancellationToken);
    Task ConnectAsync(CancellationToken cancellationToken);
}