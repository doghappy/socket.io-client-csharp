namespace SocketIOClient.Packgers
{
    public class MessageDisconnectedPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            if (string.IsNullOrEmpty(client.Namespace))
            {
                if (text == string.Empty)
                {
                    client.InvokeDisconnect("io server disconnect");
                }
            }
            else
            {
                if (text == client.Namespace)
                {
                    client.InvokeDisconnect("io server disconnect");
                }
            }
        }
    }
}
