using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RichardSzalay.MockHttp;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.TransportTests
{
    [TestClass]
    public class TransportRouterTest
    {
        //[TestMethod]
        //public async Task TestHttpConnectAsync()
        //{
        //    string uri = "http://localhost:11003/socket.io/?token=V3&EIO=4&transport=polling";

        //    var mockHttp = new MockHttpMessageHandler();
        //    mockHttp.When(uri)
        //        .Respond("text/plain", "0{\"sid\":\"BOjvjrVmDiVtT6oWAAAG\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":5000}");
        //    var httpClient = mockHttp.ToHttpClient();

        //    var clientWebSocket = new Mock<IClientWebSocket>();

        //    var uriConverter = new Mock<IUriConverter>();
        //    uriConverter
        //        .Setup(x => x.GetHandshakeUri(It.IsAny<Uri>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
        //        .Returns(new Uri(uri));

        //    IMessage message;

        //    var router = new TransportRouter(httpClient, clientWebSocket.Object)
        //    {
        //        AutoUpgrade = false,
        //        UriConverter = uriConverter.Object,
        //        OnMessageReceived = msg => message = msg
        //    };

        //    await router.ConnectAsync();

        //    Assert.AreEqual(message.Type, MessageType.Opened)
        //}
    }
}
