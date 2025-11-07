using System.Collections.Generic;
using FluentAssertions;
using SocketIOClient.V2.Protocol.Http;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Protocol.Http;

public class HttpRequestTests
{
    [Fact(Skip = "Test")]
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
            });
    }
}