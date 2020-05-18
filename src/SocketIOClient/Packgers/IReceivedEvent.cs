using System;

namespace SocketIOClient.Packgers
{
    interface IReceivedEvent : IUnpackable
    {
        string EventName { get; }
        SocketIOResponse Response { get; }

        event Action OnEnd;
    }
}
