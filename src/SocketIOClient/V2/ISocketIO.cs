using System;
using System.Threading.Tasks;
using SocketIOClient.Core.Messages;
using SocketIOClient.Transport.Http;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Session;

namespace SocketIOClient.V2;

public interface ISocketIO : IMyObserver<IMessage>
{
    IHttpClient HttpClient { get; set; }
    ISessionFactory SessionFactory { get; set; }
    int PacketId { get; }
    Task ConnectAsync();
    Task EmitAsync(string eventName, Action<IAckMessage> ack);
}