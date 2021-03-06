﻿namespace SocketIOClient.Test.SocketIOTests
{
    public interface ISocketIOCreateable
    {
        SocketIO Create();
        string Prefix { get; }
        string Url { get; }
        string Token { get; }
        int EIO { get; }
    }
}
