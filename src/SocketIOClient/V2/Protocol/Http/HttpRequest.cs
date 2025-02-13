using System;
using System.Collections.Generic;

namespace SocketIOClient.V2.Protocol.Http;

public class HttpRequest : IHttpRequest
{
    public Uri Uri { get; set; }
    public RequestMethod Method { get; set; } = RequestMethod.Get;
    public RequestBodyType BodyType { get; set; } = RequestBodyType.Text;
    public Dictionary<string, string> Headers { get; set; } = new();
    public byte[] BodyBytes { get; set; }
    public string BodyText { get; set; }
}
