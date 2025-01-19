using System;
using System.Collections.Generic;

namespace SocketIOClient.V2.Http;

public class HttpRequest:IHttpRequest
{
    public Uri Uri { get; set; }
    public RequestMethod Method { get; set; } = RequestMethod.Get;
    public RequestBodyType BodyType { get; set; } = RequestBodyType.Text;
    public Dictionary<string, string> Headers { get; set; } = new();
    public byte[] ByteBody { get; set; }
    public string TextBody { get; set; }
}
