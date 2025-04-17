using System;

namespace SocketIOClient.V2;

public interface IRandom
{
    int Next(int max);
}

public class DefaultRandom : IRandom
{
    private readonly Random _random = new();

    public int Next(int max)
    {
        return _random.Next(max);
    }
}