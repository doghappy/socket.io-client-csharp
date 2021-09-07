using Microsoft.VisualStudio.TestTools.UnitTesting;
using RichardSzalay.MockHttp;
using SocketIOClient.Transport;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.Transport
{
    [TestClass]
    public class HttpPollingTest
    {
        [TestMethod]
        public async Task Eio4Handshake()
        {
            string uri = "http://localhost:11003/socket.io/?token=V3&EIO=4&transport=polling";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond("text/plain", "0{\"sid\":\"BOjvjrVmDiVtT6oWAAAG\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}");
            var client = mockHttp.ToHttpClient();
            var transport = new HttpPolling(client);

            TransportMessage message = null;
            transport.Subscribe(msg => message = msg);

            await transport.GetAsync(uri, CancellationToken.None);

            Assert.AreEqual(TransportMessageType.Text, message.Type);
        }
    }
}
