using System.Threading;
using System.Threading.Tasks;
using NSubstitute;
using SocketIOClient.V2;
using SocketIOClient.V2.Http;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Http;

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
    public async Task SendAsync_WhenCalled_OnNextShouldBeTriggered()
    {
        var observer = Substitute.For<IProtocolMessageObserver>();
        _httpAdapter.Subscribe(observer);
        
        await _httpAdapter.SendAsync(new ProtocolMessage(), CancellationToken.None);
        
        observer.Received().OnNext(Arg.Any<ProtocolMessage>());
    }
}