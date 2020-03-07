namespace SocketIOClient.Parsers
{
    class ConnectedParser : IParser
    {
        public void Parse(ResponseTextParser rtp)
        {
            if (rtp.Text == "40" + rtp.Namespace)
            {
                rtp.ConnectHandler();
            }
            else
            {
                rtp.Parser = new DisconnectedParser();
                rtp.Parse();
            }
        }
    }
}
