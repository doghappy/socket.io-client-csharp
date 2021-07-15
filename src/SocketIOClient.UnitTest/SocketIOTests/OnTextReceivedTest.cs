using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Processors;
using SocketIOClient.WebSocketClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.SocketIOTests
{
    [TestClass]
    public class OnTextReceivedTest
    {
        [TestMethod]
        public void Text()
        {
            var mockProcessor = new Mock<Processor>();
            var mockWebSocketClient = new Mock<IWebSocketClient>();
            //mockWebSocketClient.SetupAllProperties();
            mockWebSocketClient.SetupProperty(x => x.OnTextReceived);
            var io = new SocketIO
            {
                MessageProcessor = mockProcessor.Object,
                Socket = mockWebSocketClient.Object
            };
            mockWebSocketClient.Object.OnTextReceived("test");
            mockProcessor.Verify(p => p.Process(It.IsAny<MessageContext>()), Times.Once());
            mockWebSocketClient.Verify(p => p.OnTextReceived, Times.Once());
            mockWebSocketClient.Verify(p => p.OnBinaryReceived, Times.Never());
        }
    }
}
