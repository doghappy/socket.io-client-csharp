using System;
using System.Collections.Generic;
using System.Linq;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public class EngineIO3Adapter : IEngineIOAdapter
{
    public IHttpRequest ToHttpRequest(ICollection<byte[]> bytes)
    {
        if (!bytes.Any())
        {
            throw new ArgumentException("The array cannot be empty");
        }
        foreach (var b in bytes)
        {
            if (b.Length == 0)
            {
                throw new ArgumentException("The sub array cannot be empty");
            }
        }
        throw new System.NotImplementedException();
    }
}