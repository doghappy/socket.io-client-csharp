using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;
using SocketIOClient.V2.Session.EngineIOAdapter;

namespace SocketIOClient.V2.Session.Http.EngineIOAdapter;

public class HttpEngineIO4Adapter : HttpEngineIOAdapter, IHttpEngineIOAdapter
{
    public HttpEngineIO4Adapter(
        IStopwatch stopwatch,
        IHttpAdapter httpAdapter,
        IRetriable retryPolicy,
        ILogger<HttpEngineIO4Adapter> logger,
        ISerializer serializer) : base(httpAdapter, retryPolicy, logger)
    {
        _stopwatch = stopwatch;
        _httpAdapter = httpAdapter;
        _retryPolicy = retryPolicy;
        _serializer = serializer;
    }

    private const string Delimiter = "\u001E";
    private readonly IStopwatch _stopwatch;
    private readonly IHttpAdapter _httpAdapter;
    private readonly List<IMyObserver<IMessage>> _observers = [];
    private readonly IRetriable _retryPolicy;
    private readonly ISerializer _serializer;

    public EngineIOAdapterOptions Options { get; set; }

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

    public async Task<bool> ProcessMessageAsync(IMessage message)
    {
        switch (message.Type)
        {
            case MessageType.Ping:
                await HandlePingMessageAsync().ConfigureAwait(false);
                break;
            case MessageType.Opened:
                await HandleOpenedMessageAsync(message).ConfigureAwait(false);
                break;
        }

        return false;
    }

    private async Task HandlePingMessageAsync()
    {
        _stopwatch.Restart();
        await _retryPolicy.RetryAsync(3, async () =>
        {
            using var cts = new CancellationTokenSource(Options.Timeout);
            var pong = ToHttpRequest("3");
            await _httpAdapter.SendAsync(pong, cts.Token);
        }).ConfigureAwait(false);
        _stopwatch.Stop();
        var pong = new PongMessage
        {
            Duration = _stopwatch.Elapsed,
        };
        await NotifyObserversAsync(pong).ConfigureAwait(false);
    }

    private async Task HandleOpenedMessageAsync(IMessage message)
    {
        SetOpenedMessage((OpenedMessage)message);
        var builder = new StringBuilder("40");
        if (!string.IsNullOrEmpty(Options.Namespace))
        {
            builder.Append(Options.Namespace).Append(',');
        }
        if (Options.Auth is not null)
        {
            builder.Append(_serializer.Serialize(Options.Auth));
        }

        var req = ToHttpRequest(builder.ToString());
        using var cts = new CancellationTokenSource(Options.Timeout);
        await _httpAdapter.SendAsync(req, cts.Token).ConfigureAwait(false);
    }

    private async Task NotifyObserversAsync(IMessage message)
    {
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message).ConfigureAwait(false);
        }
    }

    public void Subscribe(IMyObserver<IMessage> observer)
    {
        if (_observers.Contains(observer))
        {
            return;
        }

        _observers.Add(observer);
    }
}