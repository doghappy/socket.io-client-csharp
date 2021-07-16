using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.WebSocketClient;
using System;
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
            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new TimeoutException());

            var mockReconnectAttemp = new Mock<EventHandler<int>>();
            var mockReconnectError = new Mock<EventHandler<Exception>>();
            var mockReconnectFaild = new Mock<EventHandler>();

            using var io = new SocketIO("http://example.com");
            io.Options.ReconnectionAttempts = 1;
            io.Socket = mockSocket.Object;
            io.OnReconnectAttempt += mockReconnectAttemp.Object;
            io.OnReconnectError += mockReconnectError.Object;
            io.OnReconnectFailed += mockReconnectFaild.Object;

            await io.ConnectAsync();

            mockSocket.Verify(x => x.ConnectAsync(It.IsAny<Uri>()), Times.Exactly(2));
            mockReconnectAttemp.Verify(x => x(io, 1), Times.Once());
            mockReconnectError.Verify(x => x(io, It.IsAny<TimeoutException>()), Times.Once());
        }

        [TestMethod]
        public async Task TestAttempsEq2()
        {
            var mockSocket = new Mock<IWebSocketClient>();
            mockSocket.Setup(x => x.ConnectAsync(It.IsAny<Uri>())).Throws(new WebSocketException());

            var mockReconnectAttemp = new Mock<EventHandler<int>>();
            var mockReconnectError = new Mock<EventHandler<Exception>>();
            var mockReconnectFaild = new Mock<EventHandler>();

            using var io = new SocketIO("http://example.com");
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
        }
    }
}
