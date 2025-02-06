using System;
using System.Collections.Generic;
using FluentAssertions;
using JetBrains.Annotations;
using Mono.Collections.Generic;
using SocketIOClient.V2.Core;
using SocketIOClient.V2.UriConverter;
using Xunit;

namespace SocketIOClient.UnitTests
{
    public class DefaultUriConverterTest
    {
        private readonly DefaultUriConverter _converter = new();
        
        [Fact]
        public void GetServerUri_GivenQueryParams_AppendAsQueryString()
        {
            var serverUri = new Uri("http://localhost");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new("token", "test"),
            };
            var result = _converter.GetServerUri(false, serverUri, EngineIO.V3, string.Empty, kvs);
            result.Should().Be("http://localhost/socket.io/?EIO=3&transport=polling&token=test");
        }
        
        
        [Theory]
        [InlineData(null, "http://localhost/socket.io/?EIO=4&transport=polling")]
        [InlineData("", "http://localhost/socket.io/?EIO=4&transport=polling")]
        [InlineData(" ", "http://localhost/socket.io/?EIO=4&transport=polling")]
        [InlineData("/test", "http://localhost/test/?EIO=4&transport=polling")]
        public void GetServerUri_AppendPathToUri([CanBeNull] string path, string expected)
        {
            var serverUri = new Uri("http://localhost");
            var result = _converter.GetServerUri(false, serverUri, EngineIO.V4, path, null);
            result.Should().Be(expected);
        }

        [Theory]
        [InlineData("http://localhost:80", "http://localhost/socket.io/?EIO=4&transport=polling")]
        [InlineData("https://localhost:443", "https://localhost/socket.io/?EIO=4&transport=polling")]
        [InlineData("http://localhost:443", "http://localhost:443/socket.io/?EIO=4&transport=polling")]
        [InlineData("https://localhost:80", "https://localhost:80/socket.io/?EIO=4&transport=polling")]
        public void GetServerUri_GivenPort_ShouldNotAppearInUriIfIsDefault(string uri, string expected)
        {
            var serverUri = new Uri(uri);
            var kvs = new ReadOnlyCollection<KeyValuePair<string, string>>(Array.Empty<KeyValuePair<string, string>>());
            var result = _converter.GetServerUri(false, serverUri, EngineIO.V4, string.Empty, kvs);
            result.Should().Be(expected);
        }
        
        [Theory]
        [InlineData(false, "http://localhost","http://localhost/socket.io/?EIO=4&transport=polling")]
        [InlineData(false, "https://localhost","https://localhost/socket.io/?EIO=4&transport=polling")]
        [InlineData(true, "ws://localhost","ws://localhost/socket.io/?EIO=4&transport=websocket")]
        [InlineData(true, "wss://localhost","wss://localhost/socket.io/?EIO=4&transport=websocket")]
        public void GetServerUri_DifferentProtocol(bool ws,string uri, string expected)
        {
            var serverUri = new Uri(uri);
            var result = _converter.GetServerUri(ws, serverUri, EngineIO.V4, string.Empty, null);
            result.Should().Be(expected);
        }
    }
}
