using System.Threading.Tasks;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;

namespace SocketIOClient.V2.Session;

public interface ISession : IMessageObservable,IMyObserver<ProtocolMessage>
{
    Task ConnectAsync();
}