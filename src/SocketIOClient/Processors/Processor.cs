namespace SocketIOClient.Processors
{
    public abstract class Processor
    {
        public Processor NextProcessor { get; set; }
        public abstract void Process(MessageContext ctx);
    }
}
