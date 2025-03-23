using System.Collections.Generic;

namespace SocketIOClient.Core.Messages;

public class OpenedMessage : IMessage
{
    public MessageType Type => MessageType.Opened;
    public string Sid { get; set; }
    public int PingInterval { get; set; }
    public int PingTimeout { get; set; }
    public List<string> Upgrades { get; set; }
}