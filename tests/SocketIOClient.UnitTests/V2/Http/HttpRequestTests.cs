using System.Collections.Generic;
using FluentAssertions;
using SocketIOClient.V2;
using SocketIOClient.V2.Http;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Http;

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
                ByteBody = null,
                TextBody = null,
            });
    }
}