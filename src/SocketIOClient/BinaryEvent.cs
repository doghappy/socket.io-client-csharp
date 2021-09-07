namespace SocketIOClient
{
    public class BinaryEvent
    {
        public BinaryEvent()
        {
            PacketId = -1;
        }

        public int PacketId { get; set; }
        public string Event { get; set; }
        public int Count { get; set; }
        public SocketIOResponse Response { get; set; }
    }
}
