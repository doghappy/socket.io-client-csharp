using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using SocketIOClient.Core;
using SocketIOClient.V2.Observers;
using SocketIOClient.V2.Protocol.Http;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Protocol.Http;

public class HttpAdapterTests
{
    public HttpAdapterTests()
    {
        _httpClient = Substitute.For<IHttpClient>();
        _httpAdapter = new HttpAdapter(_httpClient);
    }

    private readonly HttpAdapter _httpAdapter;
    private readonly IHttpClient _httpClient;

    [Fact]
    public async Task SendProtocolMessageAsync_WhenCalled_OnNextShouldBeTriggered()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _httpAdapter.Subscribe(observer);

        await _httpAdapter.SendAsync(new ProtocolMessage(), CancellationToken.None);

        observer.Received().OnNextAsync(Arg.Any<ProtocolMessage>());
    }

    [Fact]
    public async Task SendHttpRequestAsync_WhenCalled_OnNextShouldBeTriggered()
    {
        var observer = Substitute.For<IMyObserver<ProtocolMessage>>();
        _httpAdapter.Subscribe(observer);

        await _httpAdapter.SendAsync(new HttpRequest(), CancellationToken.None);

        observer.Received().OnNextAsync(Arg.Any<ProtocolMessage>());
    }
}