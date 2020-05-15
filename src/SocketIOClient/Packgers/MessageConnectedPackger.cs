namespace SocketIOClient.Packgers
{
    public class MessageConnectedPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            if (string.IsNullOrEmpty(client.Namespace))
            {
                if (text == string.Empty)
                {
                    client.InvokeConnect();
                }
            }
            else
            {
                if (text == client.Namespace)
                {
                    client.InvokeConnect();
                }
            }
        }
    }
}
