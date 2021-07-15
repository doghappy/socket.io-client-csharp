using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Processors;
using Moq;
using SocketIOClient.EioHandler;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class ConnectedProcessorTest
    {
        [TestMethod]
        public void Test()
        {
            var mockEioHandler = new Mock<IEioHandler>();
            mockEioHandler
                .Setup(x => x.CheckConnection(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(new ConnectionResult
                {
                    Result = true,
                    Id = "BpXgajlbwVH1qR4QBvKz"
                });

            var processor = new ConnectedProcessor();
            ConnectionResult connectionResult = null;
            processor.Process(new MessageContext
            {
                EioHandler = mockEioHandler.Object,
                ConnectedHandler = result => connectionResult = result
            });

            mockEioHandler.Verify(x => x.CheckConnection(It.IsAny<string>(), It.IsAny<string>()), Times.Once());
            Assert.IsTrue(connectionResult.Result);
            Assert.AreEqual("BpXgajlbwVH1qR4QBvKz", connectionResult.Id);
        }
    }
}
