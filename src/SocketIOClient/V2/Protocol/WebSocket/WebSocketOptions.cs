using System.Net;
using System.Net.Security;

namespace SocketIOClient.V2.Protocol.WebSocket;

public class WebSocketOptions
{
    public IWebProxy Proxy { get; set; }
    public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
}