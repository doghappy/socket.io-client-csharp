using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RichardSzalay.MockHttp;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;

namespace SocketIOClient.UnitTest.Transport.Http
{
    [TestClass]
    public class Eio3HttpPollingHandlerTest
    {
        [TestMethod]
        [DynamicData(nameof(HttpResponseMessageCases))]
        public async Task SendHttpRequest_ReportResponse(HttpResponseMessage res, string expectedText, byte[] expectedBytes)
        {
            var mockHttp = new Mock<IHttpClient>();
            mockHttp
                .Setup(m => m.SendAsync(It.IsAny<HttpRequestMessage>(), CancellationToken.None))
                .ReturnsAsync(res);
            string text = null;
            byte[] bytes = null;
            var handler = new Eio3HttpPollingHandler(mockHttp.Object)
            {
                OnTextReceived = data => text = data,
                OnBytesReceived = data => bytes = data,
            };

            await handler.GetAsync(It.IsAny<string>(), CancellationToken.None);

            text.Should().Be(expectedText);
            bytes.Should().Equal(expectedBytes);
        }

        private static IEnumerable<object[]> HttpResponseMessageCases
        {
            get
            {
                return new[]
                {
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new StringContent(""),
                        },
                        null,
                        null,
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new StringContent("hello world!"),
                        },
                        null,
                        null,
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new StringContent("tom: hello world!"),
                        },
                        null,
                        null,
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new StringContent("1:2"),
                        },
                        "2",
                        null,
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new StringContent("96:0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}"),
                        },
                        "0{\"sid\":\"LgtKYhIy7tUzKHH9AAAB\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}",
                        null,
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 0, 4, 255, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        "test",
                        null,
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 0, 1, 2, 255, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        "testtesttest",
                        null,
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 1, 4, 255, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        null,
                        new byte[] { 0x65, 0x73, 0x74 },
                    },
                    new object[]
                    {
                        new HttpResponseMessage
                        {
                            Content = new ByteArrayContent(new byte[] { 1, 1, 2, 255, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74 })
                            {
                                Headers = { ContentType = new MediaTypeHeaderValue("application/octet-stream") },
                            },
                        },
                        null,
                        new byte[] { 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74, 0x74, 0x65, 0x73, 0x74 },
                    },
                };
            }
        }
    }
}