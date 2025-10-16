using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient.Core;
using SocketIOClient.Core.Messages;
using SocketIOClient.Serializer;
using SocketIOClient.V2.Infrastructure;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public class EngineIO4Adapter : IEngineIOAdapter
{
    public EngineIO4Adapter(
        IStopwatch stopwatch,
        IHttpAdapter httpAdapter,
        IRetriable retryPolicy)
    {
        _stopwatch = stopwatch;
        _httpAdapter = httpAdapter;
        _retryPolicy = retryPolicy;
    }

    private const string Delimiter = "\u001E";
    private readonly IStopwatch _stopwatch;
    private readonly IHttpAdapter _httpAdapter;
    private readonly List<IMyObserver<IMessage>> _observers = [];
    private readonly IRetriable _retryPolicy;

    public TimeSpan Timeout { get; set; }

    public IHttpRequest ToHttpRequest(ICollection<byte[]> bytes)
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

    public IHttpRequest ToHttpRequest(string content)
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

    public async Task ProcessMessageAsync(IMessage message)
    {
        if (message.Type == MessageType.Ping)
        {
            await HandlePingMessageAsync();
        }
    }

    private async Task HandlePingMessageAsync()
    {
        _stopwatch.Restart();
        await _retryPolicy.RetryAsync(3, async () =>
        {
            using var cts = new CancellationTokenSource(Timeout);
            var pong = ToHttpRequest("3");
            await _httpAdapter.SendAsync(pong, cts.Token);
        });
        _stopwatch.Stop();
        var pong = new PongMessage
        {
            Duration = _stopwatch.Elapsed,
        };
        await NotifyObserversAsync(pong);
    }

    private async Task NotifyObserversAsync(IMessage message)
    {
        foreach (var observer in _observers)
        {
            await observer.OnNextAsync(message);
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