using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.Http.EngineIOAdapter;

public class HttpEngineIO4Adapter : EngineIO4Adapter, IHttpEngineIOAdapter
{
    public HttpEngineIO4Adapter(
        IStopwatch stopwatch,
        IHttpAdapter httpAdapter,
        IRetriable retryPolicy,
        ISerializer serializer,
        IPollingHandler pollingHandler) : base(stopwatch, serializer)
    {
        _httpAdapter = httpAdapter;
        _retryPolicy = retryPolicy;
        _pollingHandler = pollingHandler;
    }

    private const string Delimiter = "\u001E";
    private readonly IHttpAdapter _httpAdapter;
    private readonly IRetriable _retryPolicy;
    private readonly IPollingHandler _pollingHandler;

    protected override async Task SendConnectAsync(string message)
    {
        var req = ToHttpRequest(message);
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _httpAdapter.SendAsync(req, cts.Token).ConfigureAwait(false);
    }

    protected override async Task SendPongAsync()
    {
        await _retryPolicy.RetryAsync(3, async () =>
        {
            var pong = ToHttpRequest("3");
            using var cts = new CancellationTokenSource(Options.Timeout);
            await _httpAdapter.SendAsync(pong, cts.Token);
        }).ConfigureAwait(false);
    }

    protected override void BeforeOpenedMessageHanding(OpenedMessage message)
    {
        _pollingHandler.OnOpenedMessageReceived(message);
    }

    public HttpRequest ToHttpRequest(ICollection<byte[]> bytes)
    {
        if (!bytes.Any())
        {
            throw new ArgumentException("The array cannot be empty");
        }

        var req = new HttpRequest
        {
            Method = RequestMethod.Post,
            BodyType = RequestBodyType.Text,
        };

        var base64Strings = bytes.Select(b => $"b{Convert.ToBase64String(b)}");
        req.BodyText = string.Join(Delimiter, base64Strings);
        return req;
    }

    public HttpRequest ToHttpRequest(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            throw new ArgumentException("The content cannot be null or empty");
        }

        return new HttpRequest
        {
            Method = RequestMethod.Post,
            BodyType = RequestBodyType.Text,
            BodyText = content,
        };
    }

    public IEnumerable<ProtocolMessage> ExtractMessagesFromText(string text)
    {
        var items = text.Split([Delimiter], StringSplitOptions.RemoveEmptyEntries);
        foreach (var item in items)
        {
            if (item[0] == 'b')
            {
                var bytes = Convert.FromBase64String(item.Substring(1));
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Bytes,
                    Bytes = bytes,
                };
            }
            else
            {
                yield return new ProtocolMessage
                {
                    Type = ProtocolMessageType.Text,
                    Text = item,
                };
            }
        }
    }

    public IEnumerable<ProtocolMessage> ExtractMessagesFromBytes(byte[] bytes)
    {
        return new List<ProtocolMessage>();
    }
}