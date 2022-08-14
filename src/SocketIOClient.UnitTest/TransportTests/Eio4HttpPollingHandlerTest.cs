using System;
using System.Collections.Generic;
using System.Net.Http;
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
    public class Eio4HttpPollingHandlerTest
    {
        [TestMethod]
        public async Task Should_Receive_1_Text_Message()
        {
            string uri = "http://localhost:11003/socket.io/?token=V3&EIO=4&transport=polling";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond("text/plain", "42[\"hi\",\"doghappy\"]");
            var httpClient = mockHttp.ToHttpClient();

            var msgs = new List<string>();
            var handler = new Eio4HttpPollingHandler(httpClient);
            handler.OnTextReceived += msg => msgs.Add(msg);

            await handler.GetAsync(uri, CancellationToken.None);

            Assert.AreEqual(1, msgs.Count);
            Assert.AreEqual("42[\"hi\",\"doghappy\"]", msgs[0]);
        }

        [TestMethod]
        public async Task Should_Receive_Text_And_Binary_Messages()
        {
            string uri = "http://localhost:11003/socket.io/?token=V3&EIO=4&transport=polling";

            byte[] bytes1 = Encoding.UTF8.GetBytes("first");
            byte[] bytes2 = Encoding.UTF8.GetBytes("second");
            string base1 = Convert.ToBase64String(bytes1);
            string base2 = Convert.ToBase64String(bytes2);

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond("text/plain", $"452-[\"hello world\",{{\"_placeholder\":true,\"num\":0}},{{\"code\":64,\"msg\":{{\"_placeholder\":true,\"num\":1}}}}]\u001Eb{base1}\u001Eb{base2}");
            var httpClient = mockHttp.ToHttpClient();

            var texts = new List<string>();
            var bytes = new List<byte[]>();
            var handler = new Eio4HttpPollingHandler(httpClient);
            handler.OnTextReceived += msg => texts.Add(msg);
            handler.OnBytesReceived += b => bytes.Add(b);

            await handler.GetAsync(uri, CancellationToken.None);

            Assert.AreEqual(1, texts.Count);
            Assert.AreEqual("452-[\"hello world\",{\"_placeholder\":true,\"num\":0},{\"code\":64,\"msg\":{\"_placeholder\":true,\"num\":1}}]", texts[0]);
            Assert.AreEqual(2, bytes.Count);
            Assert.AreEqual("first", Encoding.UTF8.GetString(bytes[0]));
            Assert.AreEqual("second", Encoding.UTF8.GetString(bytes[1]));
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
            var handler = new Eio4HttpPollingHandler(httpClient);

            string content = "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]";
            await handler.PostAsync(uri, content, CancellationToken.None);

            Assert.AreEqual(content, reqContent);
        }
    }
}
