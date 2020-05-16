namespace SocketIOClient.Packgers
{
    interface IReceivedEvent : IUnpackable
    {
        string EventName { get; }
        SocketIOResponse Response { get; }
    }
}
