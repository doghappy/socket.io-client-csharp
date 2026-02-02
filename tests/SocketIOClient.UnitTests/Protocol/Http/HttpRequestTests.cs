using FluentAssertions;
using SocketIOClient.Protocol.Http;

namespace SocketIOClient.UnitTests.Protocol.Http;

public class HttpRequestTests
{
    [Fact]
    public void DefaultValues()
    {
        var req = new HttpRequest();
        req.Should()
            .BeEquivalentTo(new HttpRequest
            {
                Uri = null,
                Method = RequestMethod.Get,
                Headers = new Dictionary<string, string>(),
                BodyType = RequestBodyType.Text,
                BodyBytes = null,
                BodyText = null,
                IsConnect = false,
            });
    }
}