using System.Net.Http;
using System.Net.Http.Headers;
using FluentAssertions;
using SocketIOClient.V2.Protocol.Http;
using Xunit;

namespace SocketIOClient.UnitTests.V2.Protocol.Http;

public class SystemHttpResponseTests
{
    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/html")]
    [InlineData("application/json")]
    [InlineData("application/octet-stream")]
    public void MediaType_WhenGet_AlwaysSameAsHttpResponseMessage(string mediaType)
    {
        var sysRes = new HttpResponseMessage
        {
            Content = new StringContent(string.Empty)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue(mediaType),
                },
            },
        };

        var res = new SystemHttpResponse(sysRes);

        res.MediaType.Should().Be(mediaType);
    }
}