namespace SocketIOClient.IntegrationTest.Models
{
    class ObjectResponse
    {
        public int A { get; set; }
        public string B { get; set; }
        public ObjectC C { get; set; }
    }

    class ObjectC
    {
        public string D { get; set; }
        public double E { get; set; }
    }
}
