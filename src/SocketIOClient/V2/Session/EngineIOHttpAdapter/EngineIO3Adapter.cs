using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;

namespace SocketIOClient.V2.Session.EngineIOHttpAdapter;

public class EngineIO3Adapter : IEngineIOAdapter
{
    private readonly IList<IMyObserver<string>> _textObservers = new List<IMyObserver<string>>();
    private readonly IList<IMyObserver<byte[]>> _byteObservers = new List<IMyObserver<byte[]>>();

    public IHttpRequest ToHttpRequest(ICollection<byte[]> bytes)
    {
        if (!bytes.Any())
        {
            throw new ArgumentException("The array cannot be empty");
        }
        if (bytes.Any(b => b.Length == 0))
        {
            throw new ArgumentException("The sub array cannot be empty");
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
    
    private void NotifyTextObservers(string text)
    {
        foreach (var observer in _textObservers)
        {
            observer.OnNext(text);
        }
    }

    public async Task OnNextAsync(IHttpResponse response)
    {
        var text = await response.ReadAsStringAsync();
        var p = 0;
        while (true)
        {
            var index = text.IndexOf(':', p);
            if (index == -1)
            {
                // TODO: can't handle this message
                break;
            }
            var lengthStr = text.Substring(p, index - p);
            if (int.TryParse(lengthStr, out var length))
            {
                var msg = text.Substring(index + 1, length);
                NotifyTextObservers(msg);
            }
            else
            {
                // TODO: can't handle this message
                break;
            }
            p = index + length + 1;
            if (p >= text.Length)
            {
                // TODO: can't handle this message
                break;
            }
        }
    }

    public void Subscribe(IMyObserver<string> observer)
    {
        if (_textObservers.Contains(observer))
        {
            return;
        }
        _textObservers.Add(observer);
    }

    public void Subscribe(IMyObserver<byte[]> observer)
    {
        if (_byteObservers.Contains(observer))
        {
            return;
        }
        _byteObservers.Add(observer);
    }
}