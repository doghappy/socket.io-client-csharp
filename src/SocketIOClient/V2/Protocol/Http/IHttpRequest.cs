using System;
using System.Collections.Generic;

namespace SocketIOClient.V2.Protocol.Http;

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
    Uri Uri { get; set; }
    RequestMethod Method { get; }
    RequestBodyType BodyType { get; }
    Dictionary<string, string> Headers { get; }
    byte[] BodyBytes { get; }
    string BodyText { get; }
}