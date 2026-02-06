using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Common;

namespace SocketIOClient.Protocol.Http;

public class HttpAdapter(IHttpClient httpClient, ILogger<HttpAdapter> logger) : ProtocolAdapter, IHttpAdapter
{
    public Uri? Uri { get; set; }

    public bool IsReadyToSend => Uri is not null && Uri.Query.Contains("sid=");

    private static async Task<ProtocolMessage> GetMessageAsync(IHttpResponse response)
    {
        var message = new ProtocolMessage();
        if (MediaTypeNames.Application.Octet.Equals(response.MediaType, StringComparison.InvariantCultureIgnoreCase))
        {
            message.Type = ProtocolMessageType.Bytes;
            message.Bytes = await response.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        else
        {
            message.Type = ProtocolMessageType.Text;
            message.Text = await response.ReadAsStringAsync().ConfigureAwait(false);
        }
        return message;
    }

    private async Task HandleResponseAsync(IHttpResponse response)
    {
        var incomingMessage = await GetMessageAsync(response).ConfigureAwait(false);
        await OnNextAsync(incomingMessage).ConfigureAwait(false);
    }

    public async Task SendAsync(HttpRequest req, CancellationToken cancellationToken)
    {
        req.Uri ??= NewUri();
        try
        {
            var body = req.BodyType == RequestBodyType.Text ? req.BodyText : $"0️⃣1️⃣0️⃣1️⃣ {req.BodyBytes!.Length}";
            logger.LogDebug("[Polling⬆] {Body}", body);
            var response = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            logger.LogDebug("[Polling⬇] MediaType: {media}", response.MediaType);
            _ = HandleResponseAsync(response).ConfigureAwait(false);
        }
        catch (Exception e)
        {
            logger.LogError("Failed to send http request");
            logger.LogError(e.ToString());
            if (!req.IsConnect)
            {
                OnDisconnected();
            }
            throw;
        }
    }

    private Uri NewUri()
    {
        var str = $"{Uri!.AbsoluteUri}&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return new Uri(str);
    }

    public override void SetDefaultHeader(string name, string value) => httpClient.SetDefaultHeader(name, value);
}