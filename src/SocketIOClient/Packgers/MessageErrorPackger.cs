namespace SocketIOClient.Packgers
{
    public class MessageErrorPackger : IUnpackable
    {
        public void Unpack(SocketIO client, string text)
        {
            string error = text.Trim('"');
            client.InvokeError(error);
        }
    }
}
