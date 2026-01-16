using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.Observers;

namespace SocketIOClient.Session.EngineIOAdapter;

public interface IEngineIOAdapter : IMyObservable<IMessage>
{
    EngineIOAdapterOptions Options { get; set; }
    Task<bool> ProcessMessageAsync(IMessage message);
}