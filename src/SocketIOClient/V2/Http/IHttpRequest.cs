using System;
using System.Collections.Generic;

namespace SocketIOClient.V2.Http;

public enum RequestMethod
{
    Get,
    Post,
}

public enum RequestBodyType
{
    Text,
    Bytes,
}

public interface IHttpRequest
{
    Uri Uri { get; }
    RequestMethod Method { get; }
    RequestBodyType BodyType { get; }
    Dictionary<string, string> Headers { get; }
    byte[] ByteBody { get; }
    string TextBody { get; }
}