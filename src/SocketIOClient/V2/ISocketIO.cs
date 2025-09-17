using System;
using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.Transport.Http;

namespace SocketIOClient.V2;

public interface ISocketIO
{
    IHttpClient HttpClient { get; set; }
    int PacketId { get; }
    Task ConnectAsync();
    Task EmitAsync(string eventName, Action<IDataMessage> ack);
}