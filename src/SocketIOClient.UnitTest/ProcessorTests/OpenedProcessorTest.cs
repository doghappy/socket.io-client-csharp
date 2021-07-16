using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Processors;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class OpenedProcessorTest
    {
        [TestMethod]
        public void TestOpenedProcessor()
        {
            string sid = null;
            int pingInterval = 0;
            int pingTimeout = 0;

            var processor = new OpenedProcessor();
            processor.Process(new MessageContext
            {
                Message = "{\"sid\":\"6lV4Ef7YOyGF-5dCBvKy\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}",
                OpenedHandler = (id, interval, timeout) =>
                {
                    sid = id;
                    pingInterval = interval;
                    pingTimeout = timeout;
                }
            });

            Assert.AreEqual("6lV4Ef7YOyGF-5dCBvKy", sid);
            Assert.AreEqual(10000, pingInterval);
            Assert.AreEqual(5000, pingTimeout);
        }
    }
}
