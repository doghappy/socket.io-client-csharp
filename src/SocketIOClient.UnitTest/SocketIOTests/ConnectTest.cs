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
    public class ConnectTest
    {
        [TestMethod]
        [ExpectedException(typeof(NullReferenceException))]
        public async Task TestNullReferenceException()
        {
            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new NullReferenceException());

            using var io = new SocketIO("http://example.com");
            io.Socket = mockSocket.Object;

            await io.ConnectAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task TestArgumentException()
        {
            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new ArgumentException());

            using var io = new SocketIO("http://example.com");
            io.Socket = mockSocket.Object;

            await io.ConnectAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(ApplicationException))]
        public async Task TesAtpplicationException()
        {
            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new ApplicationException());

            using var io = new SocketIO("http://example.com");
            io.Socket = mockSocket.Object;

            await io.ConnectAsync();
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public async Task TestException()
        {
            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new Exception());

            using var io = new SocketIO("http://example.com");
            io.Socket = mockSocket.Object;

            await io.ConnectAsync();
        }

        [TestMethod]
        public async Task TestAttempsEq1()
        {
            using var io = new SocketIO("http://example.com");
            var list = new List<string>();

            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new TimeoutException());

            var mockReconnectAttemp = new Mock<EventHandler<int>>();
            mockReconnectAttemp.Setup(x => x(io, It.IsAny<int>())).Callback(() => list.Add("OnReconnectAttempt"));

            var mockReconnectError = new Mock<EventHandler<Exception>>();
            mockReconnectError.Setup(x => x(io, It.IsAny<Exception>())).Callback(() => list.Add("OnReconnectError"));

            var mockReconnectFaild = new Mock<EventHandler>();
            mockReconnectFaild.Setup(x => x(io, It.IsAny<EventArgs>())).Callback(() => list.Add("OnReconnectFailed"));

            io.Options.ReconnectionAttempts = 1;
            io.Socket = mockSocket.Object;
            io.OnReconnectAttempt += mockReconnectAttemp.Object;
            io.OnReconnectError += mockReconnectError.Object;
            io.OnReconnectFailed += mockReconnectFaild.Object;

            await io.ConnectAsync();

            mockSocket.Verify(x => x.ConnectAsync(It.IsAny<Uri>()), Times.Exactly(2));
            mockReconnectAttemp.Verify(x => x(io, 1), Times.Once());
            mockReconnectError.Verify(x => x(io, It.IsAny<TimeoutException>()), Times.Once());
            mockReconnectFaild.Verify(x => x(io, It.IsAny<EventArgs>()), Times.Once());

            Assert.AreEqual(3, list.Count);
            Assert.AreEqual("OnReconnectAttempt", list[0]);
            Assert.AreEqual("OnReconnectError", list[1]);
            Assert.AreEqual("OnReconnectFailed", list[2]);
        }

        [TestMethod]
        public async Task TestAttempsEq2()
        {
            using var io = new SocketIO("http://example.com");
            var list = new List<string>();

            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new WebSocketException());

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

            await io.ConnectAsync();

            mockSocket.Verify(x => x.ConnectAsync(It.IsAny<Uri>()), Times.Exactly(3));
            mockReconnectAttemp.Verify(x => x(io, 1), Times.Once());
            mockReconnectAttemp.Verify(x => x(io, 2), Times.Once());
            mockReconnectError.Verify(x => x(io, It.IsAny<WebSocketException>()), Times.Exactly(2));
            mockReconnectFaild.Verify(x => x(io, It.IsAny<EventArgs>()), Times.Once());

            Assert.AreEqual(5, list.Count);
            Assert.AreEqual("OnReconnectAttempt", list[0]);
            Assert.AreEqual("OnReconnectError", list[1]);
            Assert.AreEqual("OnReconnectAttempt", list[2]);
            Assert.AreEqual("OnReconnectError", list[3]);
            Assert.AreEqual("OnReconnectFailed", list[4]);
        }

        [TestMethod]
        public async Task ConnectionSuccessAfterAttemp1()
        {
            using var io = new SocketIO("http://example.com");
            var list = new List<string>();

            var mockSocket = new Mock<IWebSocketClient>();
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

            await io.ConnectAsync();

            mockSocket.Verify(x => x.ConnectAsync(It.IsAny<Uri>()), Times.Exactly(2));
            mockReconnectAttemp.Verify(x => x(io, 1), Times.Once());
            mockReconnectError.Verify(x => x(io, It.IsAny<WebSocketException>()), Times.Never());
            mockReconnectFaild.Verify(x => x(io, It.IsAny<EventArgs>()), Times.Never());

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("OnReconnectAttempt", list[0]);
        }

        [TestMethod]
        public async Task ReconnectionFalse()
        {
            using var io = new SocketIO("http://example.com");
            var list = new List<string>();
            bool isThrow = false;

            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.SetupSequence(x => x.ConnectAsync(It.IsAny<Uri>()))
                .Throws(new WebSocketException())
                .Returns(Task.CompletedTask);

            var mockReconnectAttemp = new Mock<EventHandler<int>>();
            mockReconnectAttemp.Setup(x => x(io, It.IsAny<int>())).Callback(() => list.Add("OnReconnectAttempt"));

            var mockReconnectError = new Mock<EventHandler<Exception>>();
            mockReconnectError.Setup(x => x(io, It.IsAny<Exception>())).Callback(() => list.Add("OnReconnectError"));

            var mockReconnectFaild = new Mock<EventHandler>();
            mockReconnectFaild.Setup(x => x(io, It.IsAny<EventArgs>())).Callback(() => list.Add("OnReconnectFailed"));

            io.Options.Reconnection = false;
            io.Options.ReconnectionAttempts = 2;
            io.Socket = mockSocket.Object;
            io.OnReconnectAttempt += mockReconnectAttemp.Object;
            io.OnReconnectError += mockReconnectError.Object;
            io.OnReconnectFailed += mockReconnectFaild.Object;

            try
            {
                await io.ConnectAsync();
            }
            catch (WebSocketException)
            {
                isThrow = true;
            }

            Assert.IsTrue(isThrow);
            mockSocket.Verify(x => x.ConnectAsync(It.IsAny<Uri>()), Times.Once());
            mockReconnectAttemp.Verify(x => x(io, It.IsAny<int>()), Times.Never());
            mockReconnectError.Verify(x => x(io, It.IsAny<WebSocketException>()), Times.Never());
            mockReconnectFaild.Verify(x => x(io, It.IsAny<EventArgs>()), Times.Never());

            Assert.AreEqual(0, list.Count);
            //Assert.AreEqual("OnReconnectAttempt", list[0]);
        }
    }
}
