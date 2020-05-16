namespace SocketIOClient.EventArguments
{
    public class ReceivedEventArgs
    {
        public string Event { get; set; }
        public SocketIOResponse Response { get; set; }
    }
}
