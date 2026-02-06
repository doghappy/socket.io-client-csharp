using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace SocketIOClient.Protocol.Http;

public class SystemHttpClient(HttpClient http, ILogger<SystemHttpClient> logger) : IHttpClient
{
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    public void SetDefaultHeader(string name, string value)
    {
        http.DefaultRequestHeaders.TryAddWithoutValidation(name, value);
    }

    public async Task<IHttpResponse> SendAsync(HttpRequest req, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(new HttpMethod(req.Method.ToString()), req.Uri);
        request.Content = req.BodyType switch
        {
            RequestBodyType.Text => new StringContent(req.BodyText ?? string.Empty),
            RequestBodyType.Bytes => new ByteArrayContent(req.BodyBytes!),
            _ => throw new NotSupportedException(),
        };

        SetHeaders(req, request);

        var res = await http.SendAsync(request, cancellationToken).ConfigureAwait(false);
#if DEBUG
        logger.LogDebug("===============DEBUG===============");
        logger.LogDebug("Uri: {uri} StatusCode: {code} ", req.Uri, res.StatusCode);
        foreach (var header in res.Headers)
        {
            logger.LogDebug("Header: {header} Value: {value}", header.Key, header.Value);
        }
#endif
        return new SystemHttpResponse(res);
    }

    private static void SetHeaders(HttpRequest req, HttpRequestMessage request)
    {
        var content = (ByteArrayContent)request.Content!;
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