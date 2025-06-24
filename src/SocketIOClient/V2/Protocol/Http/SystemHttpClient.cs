using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public class SystemHttpClient(HttpMessageInvoker http) : IHttpClient
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    // public async Task<IHttpResponse> SendAsync(IHttpRequest req)
    // {
    //     using var cts = new CancellationTokenSource(Timeout);
    //     return await SendAsync(req, cts.Token);
    // }

    public async Task<IHttpResponse> SendAsync(IHttpRequest req, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(new HttpMethod(req.Method.ToString()), req.Uri);
        request.Content = req.BodyType switch
        {
            RequestBodyType.Text => new StringContent(req.BodyText ?? string.Empty),
            RequestBodyType.Bytes => new ByteArrayContent(req.BodyBytes),
            _ => throw new NotSupportedException(),
        };

        SetHeaders(req, request);

        var res = await http.SendAsync(request, cancellationToken);
        return new SystemHttpResponse(res);
    }

    private static void SetHeaders(IHttpRequest req, HttpRequestMessage request)
    {
        var content = (ByteArrayContent)request.Content;
        foreach (var header in req.Headers)
        {
            if (HttpHeaders.ContentType.Equals(header.Key))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue(header.Value);
                continue;
            }
            request.Headers.Add(header.Key, header.Value);
        }
    }
}