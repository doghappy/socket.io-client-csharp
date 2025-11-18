using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Protocol.Http;

public class HttpAdapter(IHttpClient httpClient, ILogger<HttpAdapter> logger) : IHttpAdapter
{
    private readonly List<IMyObserver<ProtocolMessage>> _observers = [];

    public Uri Uri { get; set; }

    public bool IsReadyToSend => Uri is not null && Uri.Query.Contains("sid=");

    private async Task<IHttpResponse> SendProtocolMessageAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        var req = new HttpRequest
        {
            Method = RequestMethod.Post,
            Uri = Uri,
        };

        if (message.Type == ProtocolMessageType.Text)
        {
            req.BodyText = message.Text;
            req.BodyType = RequestBodyType.Text;
        }
        else
        {
            req.BodyBytes = message.Bytes;
            req.BodyType = RequestBodyType.Bytes;
        }
        return await httpClient.SendAsync(req, cancellationToken);
    }

    private static async Task<ProtocolMessage> GetMessageAsync(IHttpResponse response)
    {
        var message = new ProtocolMessage();
        if (response.MediaType.Equals(MediaTypeNames.Application.Octet, StringComparison.InvariantCultureIgnoreCase))
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
#if DEBUG
        var body = incomingMessage.Type == ProtocolMessageType.Text
            ? incomingMessage.Text
            : $"0️⃣1️⃣0️⃣1️⃣ {incomingMessage.Bytes!.Length}";
        logger.LogDebug("[Http⬇] {Body}", body);
#endif
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(incomingMessage).ConfigureAwait(false);
        }
    }

    public async Task SendAsync(HttpRequest req, CancellationToken cancellationToken)
    {
        req.Uri ??= NewUri();
        var response = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
#if DEBUG
        var body = req.BodyType == RequestBodyType.Text ? req.BodyText : $"0️⃣1️⃣0️⃣1️⃣ {req.BodyBytes!.Length}";
        logger.LogDebug("[Http⬆] {Body}", body);
#endif
        _ = HandleResponseAsync(response).ConfigureAwait(false);
    }

    private Uri NewUri()
    {
        var str = $"{Uri.AbsoluteUri}&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
        return new Uri(str);
    }

    public void Subscribe(IMyObserver<ProtocolMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }

    public void SetDefaultHeader(string name, string value) => httpClient.SetDefaultHeader(name, value);
}