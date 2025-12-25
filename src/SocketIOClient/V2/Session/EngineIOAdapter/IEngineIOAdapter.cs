using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public interface IEngineIOAdapter : IMyObservable<IMessage>
{
    EngineIOAdapterOptions Options { get; set; }
    Task<bool> ProcessMessageAsync(IMessage message);
}