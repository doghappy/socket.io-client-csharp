using System;
using System.Net.Http;
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
        foreach (var header in req.Headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        switch (req.BodyType)
        {
            case RequestBodyType.Text:
                if (!string.IsNullOrEmpty(req.BodyText))
                    request.Content = new StringContent(req.BodyText);
                break;
            case RequestBodyType.Bytes:
                request.Content = new ByteArrayContent(req.BodyBytes);
                break;
            default:
                throw new NotSupportedException();
        }

        var res = await http.SendAsync(request, cancellationToken);
        return new SystemHttpResponse(res);
    }
}