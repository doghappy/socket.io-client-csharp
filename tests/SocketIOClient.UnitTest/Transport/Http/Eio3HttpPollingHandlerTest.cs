using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Transport.Http;

namespace SocketIOClient.UnitTest.Transport.Http
{
    [TestClass]
    public class Eio3HttpPollingHandlerTest
    {
        [TestMethod]
        [DynamicData(nameof(SendAsyncCases))]
        public async Task SendHttpRequest_ReportReceived(HttpResponseMessage res, IEnumerable<string> expectedTexts, IEnumerable<byte[]> expectedBytes)
        {
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None))
                .ReturnsAsync(res);
            var texts = new List<string>();
            var bytes = new List<byte[]>();
            var handler = new Eio3HttpPollingHandler(mockHttp.Object)
            {
                OnTextReceived = data =>
                {
                    texts.Add(data);
                    return Task.CompletedTask;
                },
                OnBytesReceived = data => bytes.Add(data),
            };

            await handler.GetAsync(It.IsAny<string>(), CancellationToken.None);

            texts.Should().Equal(expectedTexts);
            bytes.Should().BeEquivalentTo(expectedBytes, options => options.WithStrictOrdering());
        }

        private static IEnumerable<object[]> SendAsyncCases => SendAsyncTupleCases.Select(x => new object[] { x.res, x.texts, x.bytes });

        private static IEnumerable<(HttpResponseMessage res, IEnumerable<string> texts, IEnumerable<byte[]> bytes)> SendAsyncTupleCases
        {
            get
            {
                return new (HttpResponseMessage res, IEnumerable<string> texts, IEnumerable<byte[]> bytes)[]
                {
                    (
                        new HttpResponseMessage
                        {
                            Content = new StringContent(""),
                        },
                        Array.Empty<string>(),
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new StringContent("hello world!"),
                        },
                        Array.Empty<string>(),
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new StringContent("tom: hello world!"),
                        },
                        Array.Empty<string>(),
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new StringContent("1:2"),
                        },
                        new[] { "2" },
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new StringContent("96:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}"),
                        },
                        new[] { "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}" },
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 0, 4, 255, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        new[] { "test" },
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 0, 1, 2, 255, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        new[] { "testtesttest" },
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 1, 4, 255, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        Array.Empty<string>(),
                        new[] { new byte[] { 0x65, 0x73, 0x74 } }),
                    (
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 1, 1, 2, 255, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        Array.Empty<string>(),
                        new[] { new byte[] { 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74 } }),
                    (
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 0, 1, 255, 0x32, 0, 1, 2, 255, 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        new[] { "2", "🦊🐶🐱" },
                        Array.Empty<byte[]>()),
                    (
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 0, 1, 255, 0x32, 0, 1, 2, 255, 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1, 1, 2, 255, 250, 0x32, 1, 1, 3, 255, 222, 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        new[] { "2", "🦊🐶🐱" },
                        new[] { new byte[] { 0x32 }, new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 } }),
                };
            }
        }

        [TestMethod]
        [DynamicData(nameof(PostBytesAsyncCases))]
        public async Task PostBytes_FormatBytes(IEnumerable<byte[]> bytes, IEnumerable<byte> expectedBytes)
        {
            HttpContent actualContent = null;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), CancellationToken.None))
                .Callback<string, HttpContent, CancellationToken>((u, c, t) => actualContent = c);
            var handler = new Eio3HttpPollingHandler(mockHttp.Object);

            await handler.PostAsync(It.IsAny<string>(), bytes, CancellationToken.None);

            actualContent.Should().NotBeNull();
            var actualBytes = await actualContent.ReadAsByteArrayAsync();
            actualBytes.Should().Equal(expectedBytes);
        }

        private static IEnumerable<object[]> PostBytesAsyncCases => PostBytesAsyncTupleCases.Select(x => new object[] { x.bytes, x.expectedBytes });

        private static IEnumerable<(IEnumerable<byte[]> bytes, IEnumerable<byte> expectedBytes)> PostBytesAsyncTupleCases
        {
            get
            {
                return new (IEnumerable<byte[]> bytes, IEnumerable<byte> expectedBytes)[]
                {
                    (new[] { new byte[] { 1 } }, new byte[] { 1, 2, 255, 4, 1 }),
                    (new[] { new byte[] { 255, 1 } }, new byte[] { 1, 3, 255, 4, 255, 1 }),
                    (new[] { new byte[] { 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1 }, new byte[] { 255, 1 } },
                        new byte[] { 1, 1, 3, 255, 4, 0xf0, 0x9f, 0xa6, 0x8a, 0xf0, 0x9f, 0x90, 0xb6, 0xf0, 0x9f, 0x90, 0xb1, 1, 3, 255, 4, 255, 1 }),
                };
            }
        }

        [TestMethod]
        [DataRow(null, null)]
        [DataRow("", null)]
        [DataRow("2", "1:2")]
        [DataRow("55c0ddeacd70418ab34de965de54a417", "32:55c0ddeacd70418ab34de965de54a417")]
        public async Task PostText_FormatText(string text, string expected)
        {
            string actual = null;
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.PostAsync(It.IsAny<string>(), It.IsAny<HttpContent>(), CancellationToken.None))
                .ReturnsAsync(new HttpResponseMessage { Content = new StringContent("ok") })
                .Callback<string, HttpContent, CancellationToken>(async (u, c, t) => actual = await c.ReadAsStringAsync(t));
            var handler = new Eio3HttpPollingHandler(mockHttp.Object);

            await handler.PostAsync(It.IsAny<string>(), text, CancellationToken.None);

            actual.Should().Be(expected);
        }
    }
}