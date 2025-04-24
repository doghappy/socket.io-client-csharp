using System;

namespace SocketIOClient.V2.Infrastructure;

public interface IRandom
{
    int Next(int max);
}