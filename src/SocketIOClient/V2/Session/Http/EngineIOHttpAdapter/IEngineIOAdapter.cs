using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.V2.Session.Http.EngineIOHttpAdapter;

public interface IEngineIOAdapter : IMyObservable<IMessage>
{
    TimeSpan Timeout { get; set; }
    HttpRequest ToHttpRequest(ICollection<byte[]> bytes);
    HttpRequest ToHttpRequest(string content);
    IEnumerable<ProtocolMessage> ExtractMessagesFromText(string text);
    IEnumerable<ProtocolMessage> ExtractMessagesFromBytes(byte[] bytes);
    Task ProcessMessageAsync(IMessage message);
}