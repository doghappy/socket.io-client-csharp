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

public class EngineIO3Adapter : IEngineIOAdapter, IDisposable
{
    public EngineIO3Adapter(
        IStopwatch stopwatch,
        ISerializer serializer,
        IProtocolAdapter protocolAdapter,
        TimeSpan timeout,
        IRetriable retryPolicy)
    {
        _stopwatch = stopwatch;
        _serializer = serializer;
        _protocolAdapter = protocolAdapter;
        _timeout = timeout;
        _retryPolicy = retryPolicy;
    }

    private readonly IStopwatch _stopwatch;
    private readonly ISerializer _serializer;
    private readonly IProtocolAdapter _protocolAdapter;
    private readonly CancellationTokenSource _pingCancellationTokenSource = new();
    private OpenedMessage _openedMessage;

    private readonly List<IMyObserver<IMessage>> _observers = [];
    private readonly TimeSpan _timeout;
    private readonly IRetriable _retryPolicy;

    public IHttpRequest ToHttpRequest(ICollection<byte[]> bytes)
    {
        if (!bytes.Any())
        {
            throw new ArgumentException("The array cannot be empty");
        }
        var req = new HttpRequest
        {
            Method = RequestMethod.Post,
            BodyType = RequestBodyType.Bytes,
            Headers = new Dictionary<string, string>
            {
                { HttpHeaders.ContentType, MediaTypeNames.Application.Octet },
            },
        };
        var capacity = bytes.Sum(x => x.Length + 16);
        var list = new List<byte>(capacity);
        foreach (var b in bytes)
        {
            list.Add(1);
            list.AddRange(b.Length.ToString().Select(c => Convert.ToByte(c - '0')));
            list.Add(byte.MaxValue);
            list.Add(4);
            list.AddRange(b);
        }
        req.BodyBytes = list.ToArray();
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
            BodyText = $"{content.Length}:{content}",
        };
    }

    public IEnumerable<ProtocolMessage> GetMessages(string text)
    {
        var p = 0;
        while (true)
        {
            var index = text.IndexOf(':', p);
            if (index == -1)
            {
                break;
            }
            var lengthStr = text.Substring(p, index - p);
            if (int.TryParse(lengthStr, out var length))
            {
                var msg = text.Substring(index + 1, length);
                yield return new ProtocolMessage { Text = msg };
            }
            else
            {
                break;
            }
            p = index + length + 1;
            if (p >= text.Length)
            {
                break;
            }
        }
    }

    public async Task ProcessMessageAsync(IMessage message)
    {
        switch (message.Type)
        {
            case MessageType.Opened:
                _openedMessage = (OpenedMessage)message;
                break;
            case MessageType.Connected:
                _ = Task.Run(StartPingAsync);
                break;
            case MessageType.Pong:
                await HandlePongMessageAsync(message);
                break;
        }
    }

    private async Task HandlePongMessageAsync(IMessage message)
    {
        _stopwatch.Stop();
        var pongMessage = (PongMessage)message;
        pongMessage.Duration = _stopwatch.Elapsed;
        await NotifyObserversAsync(pongMessage);
    }

    private async Task StartPingAsync()
    {
        var token = _pingCancellationTokenSource.Token;
        while (!token.IsCancellationRequested)
        {
            await Task.Delay(_openedMessage.PingInterval, token);
            var ping = _serializer.NewPingMessage();
            await _retryPolicy.RetryAsync(3, async () =>
            {
                using var cts = new CancellationTokenSource(_timeout);
                await _protocolAdapter.SendAsync(ping, cts.Token);
            });
            _stopwatch.Restart();
            _ = NotifyObserversAsync(new PingMessage());
        }
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

    public void Dispose()
    {
        _pingCancellationTokenSource.Cancel();
        _pingCancellationTokenSource.Dispose();
    }
}