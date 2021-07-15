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
    }
}
