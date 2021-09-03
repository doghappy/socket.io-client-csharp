namespace SocketIOClient.Converters
{
    public interface ICvtMessage
    {
        CvtMessageType Type { get; }

        void Read(string msg);

        string Write();
    }
}
