using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Transport.Http;

namespace SocketIOClient.UnitTests.Transport.Http
{
    [TestClass]
    public class Eio4HttpPollingHandlerTest
    {
        [TestMethod]
        [DynamicData(nameof(PostBytesAsyncCases))]
        public async Task PostBytes_FormatBytes(IEnumerable<byte[]> bytes, string expected)
        {
            string actual = null;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), CancellationToken.None))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("ok") })
                .Callback<string, HttpContent, CancellationToken>(async (u, c, t) => actual = await c.ReadAsStringAsync(t));
            var handler = new Eio4HttpPollingHandler(mockHttp.Object);

            await handler.PostAsync(It.IsAny<string>(), bytes, CancellationToken.None);

            actual.Should().Be(expected);
        }

        private static IEnumerable<object[]> PostBytesAsyncCases => PostBytesAsyncTupleCases.Select(x => new object[] { x.bytes, x.expected });

        private static IEnumerable<(IEnumerable<byte[]> bytes, string expected)> PostBytesAsyncTupleCases
        {
            get
            {
                return new (IEnumerable<byte[]> bytes, string expected)[]
                {
                    (new[] { new byte[] { 1 } }, "bAQ=="),
                    (new[] { new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 }, new byte[] { 255, 1 } }, "b8J+mivCfkLbwn5Cx\u001Eb/wE="),
                };
            }
        }
    }
}