using SocketIOClient.EioHandler;

namespace SocketIOClient.Packgers
{
    public class MessageConnectedPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            client.Options.EioHandler.Unpack(client, text);
        }
    }
}
