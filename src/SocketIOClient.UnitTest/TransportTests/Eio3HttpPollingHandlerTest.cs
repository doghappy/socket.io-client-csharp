using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using SocketIOClient.Transport;
using SocketIOClient.Transport.Http;

namespace SocketIOClient.UnitTest.TransportTests
{
    [TestClass]
    public class Eio3HttpPollingHandlerTest
    {
        [TestMethod]
        public async Task Should_Receive_1_Text_Message()
        {
            string uri = "http://localhost:11002/socket.io/?token=V3&EIO=3&transport=polling";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond("text/plain", "2:40");
            var httpClient = mockHttp.ToHttpClient();

            var msgs = new List<string>();
            var handler = new Eio3HttpPollingHandler(httpClient);
            handler.OnTextReceived += msg => msgs.Add(msg);

            await handler.GetAsync(uri, CancellationToken.None);

            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual("40", msgs[0]);
        }

        [TestMethod]
        public async Task Should_Receive_2_Text_Messages()
        {
            string uri = "http://localhost:11002/socket.io/?token=V3&EIO=3&transport=polling";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond("text/plain", "2:4024:42[\"hi\",\"doghappy:test\"]");
            var httpClient = mockHttp.ToHttpClient();

            var msgs = new List<string>();
            var handler = new Eio3HttpPollingHandler(httpClient);
            handler.OnTextReceived += msg => msgs.Add(msg);

            await handler.GetAsync(uri, CancellationToken.None);

            Assert.AreEqual(2, msgs.Count);
            Assert.AreEqual("40", msgs[0]);
            Assert.AreEqual("42[\"hi\",\"doghappy:test\"]", msgs[1]);
        }

        [TestMethod]
        public async Task Should_Receive_Text_And_Binary_Messages()
        {
            string uri = "http://localhost:11002/socket.io/?token=V3&EIO=3&transport=polling";
            byte[] arrOutput = { 0x00, 0x07, 0x05, 0xFF, 0x34, 0x35, 0x32, 0x2D, 0x5B, 0x22, 0x77, 0x65, 0x6C, 0x63, 0x6F, 0x6D, 0x65, 0x22, 0x2C, 0x7B, 0x22, 0x5F, 0x70, 0x6C, 0x61, 0x63, 0x65, 0x68, 0x6F, 0x6C, 0x64, 0x65, 0x72, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x2C, 0x22, 0x6E, 0x75, 0x6D, 0x22, 0x3A, 0x30, 0x7D, 0x2C, 0x7B, 0x22, 0x5F, 0x70, 0x6C, 0x61, 0x63, 0x65, 0x68, 0x6F, 0x6C, 0x64, 0x65, 0x72, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x2C, 0x22, 0x6E, 0x75, 0x6D, 0x22, 0x3A, 0x31, 0x7D, 0x5D, 0x01, 0x02, 0x09, 0xFF, 0x04, 0x77, 0x65, 0x6C, 0x63, 0x6F, 0x6D, 0x65, 0x20, 0x42, 0x71, 0x46, 0x63, 0x32, 0x53, 0x6F, 0x72, 0x67, 0x75, 0x6B, 0x4F, 0x32, 0x36, 0x2D, 0x6C, 0x41, 0x41, 0x41, 0x4A, 0x01, 0x05, 0xFF, 0x04, 0x74, 0x65, 0x73, 0x74 };
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond(req =>
                {
                    var content = new ByteArrayContent(arrOutput);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    return new HttpResponseMessage
                    {
                        Content = content
                    };
                });
            var httpClient = mockHttp.ToHttpClient();

            var texts = new List<string>();
            var bytes = new List<byte[]>();
            var handler = new Eio3HttpPollingHandler(httpClient);
            handler.OnTextReceived += msg => texts.Add(msg);
            handler.OnBytesReceived +=  b => bytes.Add(b);

            await handler.GetAsync(uri, CancellationToken.None);

            Assert.AreEqual(1, texts.Count);
            Assert.AreEqual("452-[\"welcome\",{\"_placeholder\":true,\"num\":0},{\"_placeholder\":true,\"num\":1}]", texts[0]);
            Assert.AreEqual(2, bytes.Count);
            Assert.AreEqual("welcome BqFc2SorgukO26-lAAAJ", Encoding.UTF8.GetString(bytes[0]));
            Assert.AreEqual("test", Encoding.UTF8.GetString(bytes[1]));
        }

        [TestMethod]
        public async Task Post_Binary_Messages_Should_Work()
        {
            string uri = "http://localhost:11002/socket.io/?token=V3&EIO=3&transport=polling";
            var mockHttp = new MockHttpMessageHandler();
            string contentType = null;
            byte[] outgoing = null;
            mockHttp.When(uri)
                .Respond(req =>
                {
                    contentType = req.Content.Headers.ContentType.MediaType;
                    outgoing = req.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
                    return new StringContent(string.Empty, Encoding.UTF8, "text/plain");
                });
            var httpClient = mockHttp.ToHttpClient();
            var handler = new Eio3HttpPollingHandler(httpClient);

            var bytes = new List<byte[]>();
            var item0 = Encoding.UTF8.GetBytes("hello world 你好世界 hello world");
            bytes.Add(item0);
            await handler.PostAsync(uri, bytes, CancellationToken.None);

            Assert.AreEqual("application/octet-stream", contentType);
            Assert.AreEqual(1, outgoing[0]);
            int item0Length = item0.Length + 1;
            Assert.AreEqual(item0Length / 10, outgoing[1]);
            Assert.AreEqual(item0Length % 10, outgoing[2]);
            Assert.AreEqual(byte.MaxValue, outgoing[3]);
            Assert.AreEqual(4, outgoing[4]);
            Assert.IsTrue(Enumerable.SequenceEqual(item0, outgoing.Skip(5)));
        }

        [TestMethod]
        public async Task Post_Text_Messages_Should_Work()
        {
            string uri = "http://localhost:11002/socket.io/?token=V3&EIO=3&transport=polling";
            var mockHttp = new MockHttpMessageHandler();
            string reqContent = null;
            mockHttp.When(uri)
                .Respond(req =>
                {
                    reqContent = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    return new StringContent(string.Empty, Encoding.UTF8, "text/plain");
                });
            var httpClient = mockHttp.ToHttpClient();
            var handler = new Eio3HttpPollingHandler(httpClient);

            string content = "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]";
            await handler.PostAsync(uri, content, CancellationToken.None);

            Assert.AreEqual($"{content.Length}:{content}", reqContent);
        }
    }
}
