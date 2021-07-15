using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Processors;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class DisconnectedProcessorTest
    {
        [TestMethod]
        public void TestWithoutNamespace()
        {
            bool called = false;

            var processor = new DisconnectedProcessor();
            processor.Process(new MessageContext
            {
                Message = string.Empty,
                DisconnectedHandler = () => called = true
            });

            Assert.IsTrue(called);
        }


        [TestMethod]
        public void TestWithNamespace()
        {
            bool called = false;

            var processor = new DisconnectedProcessor();
            processor.Process(new MessageContext
            {
                Namespace = "/github",
                Message = "/github,",
                DisconnectedHandler = () => called = true
            });

            Assert.IsTrue(called);
        }
    }
}
