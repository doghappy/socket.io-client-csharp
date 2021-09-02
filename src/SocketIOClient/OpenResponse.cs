namespace SocketIOClient
{
    public class OpenResponse
    {
        public string Sid { get; set; }

        //public List<string> Upgrades { get; set; }

        public int PingInterval { get; set; }

        public int PingTimeout { get; set; }
    }
}
