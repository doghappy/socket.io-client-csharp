using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.SocketIOTests
{
    [TestClass]
    public class ReconnectionTest
    {
        [TestMethod]
        public async Task ReonnectionSuccessAfterAttemp2()
        {
            using var io = new SocketIO("http://example.com");
            var list = new List<string>();

            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.SetupProperty(x => x.OnClosed);
            mockSocket.SetupProperty(x => x.OnTextReceived);
            mockSocket.SetupSequence(x => x.ConnectAsync(It.IsAny<Uri>()))
                .Throws(new WebSocketException())
                .Returns(Task.CompletedTask);

            var mockReconnectAttemp = new Mock<EventHandler<int>>();
            mockReconnectAttemp.Setup(x => x(io, It.IsAny<int>())).Callback(() => list.Add("OnReconnectAttempt"));

            var mockReconnectError = new Mock<EventHandler<Exception>>();
            mockReconnectError.Setup(x => x(io, It.IsAny<Exception>())).Callback(() => list.Add("OnReconnectError"));

            var mockReconnectFaild = new Mock<EventHandler>();
            mockReconnectFaild.Setup(x => x(io, It.IsAny<EventArgs>())).Callback(() => list.Add("OnReconnectFailed"));

            io.Options.ReconnectionAttempts = 2;
            io.Socket = mockSocket.Object;
            io.OnReconnectAttempt += mockReconnectAttemp.Object;
            io.OnReconnectError += mockReconnectError.Object;
            io.OnReconnectFailed += mockReconnectFaild.Object;

            mockSocket.Object.OnTextReceived("40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");
            mockSocket.Object.OnClosed("xxx");
            await Task.Delay(2000);

            mockSocket.Verify(x => x.ConnectAsync(It.IsAny<Uri>()), Times.Exactly(2));
            mockReconnectAttemp.Verify(x => x(io, 1), Times.Once());
            mockReconnectAttemp.Verify(x => x(io, 2), Times.Once());
            mockReconnectError.Verify(x => x(io, It.IsAny<WebSocketException>()), Times.Once());
            mockReconnectFaild.Verify(x => x(io, It.IsAny<EventArgs>()), Times.Never());

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("OnReconnectAttempt", list[0]);
            Assert.AreEqual("OnReconnectError", list[1]);
            Assert.AreEqual("OnReconnectAttempt", list[2]);
        }
    }
}
