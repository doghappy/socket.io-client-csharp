namespace SocketIOClient.Parsers
{
    class DisconnectedParser : IParser
    {
        public void Parse(ResponseTextParser rtp)
        {
            if (rtp.Text == "41" + rtp.Namespace)
            {
                rtp.CloseHandler();
            }
            else
            {
                rtp.Parser = new MessageEventParser();
                rtp.Parse();
            }
        }
    }
}
