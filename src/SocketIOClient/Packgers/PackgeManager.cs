using System;

namespace SocketIOClient.Packgers
{
    public class PackgeManager
    {
        public PackgeManager(SocketIO client)
        {
            _client = client;
        }

        readonly SocketIO _client;

        public void Unpack(string msg)
        {
            if (!string.IsNullOrWhiteSpace(msg))
            {
                string identity = msg[0].ToString();
                string content = msg.Substring(1);
                if (Enum.TryParse(identity, out EngineIOProtocol protocol))
                {
                    IUnpackable unpackger = null;
                    switch (protocol)
                    {
                        case EngineIOProtocol.Open:
                            unpackger = new OpenedPackger();
                            break;
                        case EngineIOProtocol.Close:
                            break;
                        case EngineIOProtocol.Ping:
                            unpackger = new PingPackger();
                            break;
                        case EngineIOProtocol.Pong:
                            unpackger = new PongPackger();
                            break;
                        case EngineIOProtocol.Message:
                            unpackger = new MessagePackger();
                            break;
                        case EngineIOProtocol.Upgrade:
                            break;
                        case EngineIOProtocol.Noop:
                            break;
                        default:
                            break;
                    }
                    if (unpackger != null)
                    {
                        unpackger.Unpack(_client, content);
                    }
                }
            }
        }
    }
}
