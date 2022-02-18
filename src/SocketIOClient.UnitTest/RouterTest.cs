using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.UriConverters;
using SocketIOClient.Routers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketIOClient.Transport;
using RichardSzalay.MockHttp;

namespace SocketIOClient.UnitTest
{
    [TestClass]
    public class RouterTest
    {
        [TestMethod]
        public async Task V2_Server_Only_Support_Polling_ReturnsPolling()
        {
            string uri = "ws://localhost:11002/socket.io/?token=V2&EIO=3&transport=polling";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp
                .When(uri)
                .Respond("application/json", "85:0{\"sid\":\"LvT3j_n54ltd7NtZAAAA\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}");
            var httpClient = mockHttp.ToHttpClient();
            TransportProtocol protocol = await Router.GetProtocolAsync(httpClient, new Uri(uri));
            Assert.AreEqual(TransportProtocol.Polling, protocol);
        }

        [TestMethod]
        public async Task V2_Server_Support_Polling_And_WebSocket_ReturnsWebSocket()
        {
            string uri = "ws://localhost:11002/socket.io/?token=V2&EIO=3&transport=polling";
            var mockHttp = new MockHttpMessageHandler();
            mockHttp
                .When(uri)
                .Respond("application/json", "85:0{\"sid\":\"LvT3j_n54ltd7NtZAAAA\",\"upgrades\":[\"websocket\"],\"pingInterval\":10000,\"pingTimeout\":5000}");
            var httpClient = mockHttp.ToHttpClient();

            TransportProtocol protocol = await Router.GetProtocolAsync(httpClient, new Uri(uri));
            Assert.AreEqual(TransportProtocol.WebSocket, protocol);
        }
    }
}