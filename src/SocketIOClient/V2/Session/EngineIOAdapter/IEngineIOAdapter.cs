using System;
using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Session.EngineIOAdapter;

public interface IEngineIOAdapter : IMyObservable<IMessage>
{
    TimeSpan Timeout { get; set; }
    string Namespace { get; set; }
    Task<bool> ProcessMessageAsync(IMessage message);
}