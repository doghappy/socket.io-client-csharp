using System.Net;
using System.Net.Security;

namespace SocketIOClient.Protocol.WebSocket;

public class WebSocketOptions
{
    public IWebProxy Proxy { get; set; }
    public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }
}