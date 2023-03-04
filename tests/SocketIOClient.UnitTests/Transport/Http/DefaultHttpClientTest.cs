using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Transport.Http;

namespace SocketIOClient.UnitTests.Transport.Http;

[TestClass]
public class DefaultHttpClientTest
{
    [TestMethod]
    public void Should_update_header_value_when_header_key_exists()
    {
        var http = new DefaultHttpClient();
        http.AddHeader("key", "value1");
        http.AddHeader("key", "value2");
        http.GetHeaderValues("key").Should().Equal("value2");
    }
}