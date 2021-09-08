namespace SocketIOClient.Messages
{
    public interface IMessage
    {
        MessageType Type { get; }

        void Read(string msg);

        //void Eio3WsRead(string msg);

        //void Eio3HttpRead(string msg);

        string Write();

        //string Eio3WsWrite();

        //string Eio3HttpWrite();
    }
}
