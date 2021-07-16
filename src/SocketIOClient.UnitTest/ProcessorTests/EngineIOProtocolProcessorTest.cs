using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Processors;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class EngineIOProtocolProcessorTest
    {
        [TestMethod]
        public void Open()
        {
            var mockOpenedHandler = new Mock<OnOpened>();

            var processor = new EngineIOProtocolProcessor();
            processor.Process(new MessageContext
            {
                Message = "0{\"sid\":\"6lV4Ef7YOyGF-5dCBvKy\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                OpenedHandler = mockOpenedHandler.Object
            });

            Assert.AreEqual(typeof(OpenedProcessor), processor.NextProcessor.GetType());
            mockOpenedHandler.Verify(x => x(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()), Times.Once());
        }

        [TestMethod]
        public void Ping()
        {
            var mockPingHandler = new Mock<OnPing>();

            var processor = new EngineIOProtocolProcessor();
            processor.Process(new MessageContext
            {
                Message = "2",
                PingHandler = mockPingHandler.Object
            });

            Assert.AreEqual(typeof(PingProcessor), processor.NextProcessor.GetType());
            mockPingHandler.Verify(x => x(), Times.Once());
        }

        [TestMethod]
        public void Pong()
        {
            var mockPongHandler = new Mock<OnPong>();

            var processor = new EngineIOProtocolProcessor();
            processor.Process(new MessageContext
            {
                Message = "3",
                PongHandler = mockPongHandler.Object
            });

            Assert.AreEqual(typeof(PongProcessor), processor.NextProcessor.GetType());
            mockPongHandler.Verify(x => x(), Times.Once());
        }
    }
}
