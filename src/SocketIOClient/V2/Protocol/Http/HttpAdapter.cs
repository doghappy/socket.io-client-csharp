using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.V2.Observers;

namespace SocketIOClient.V2.Protocol.Http;

public class HttpAdapter(IHttpClient httpClient) : IHttpAdapter
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

    public async Task SendAsync(ProtocolMessage message, CancellationToken cancellationToken)
    {
        var response = await SendProtocolMessageAsync(message, cancellationToken);
        await HandleResponseAsync(response);
    }

    private async Task HandleResponseAsync(IHttpResponse response)
    {
        var incomingMessage = await GetMessageAsync(response).ConfigureAwait(false);
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(incomingMessage).ConfigureAwait(false);
        }
    }

    public async Task SendAsync(IHttpRequest req, CancellationToken cancellationToken)
    {
        req.Uri ??= Uri;
        var response = await httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
        _ = HandleResponseAsync(response).ConfigureAwait(false);
    }

    public void Subscribe(IMyObserver<ProtocolMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }
        _observers.Add(observer);
    }
}