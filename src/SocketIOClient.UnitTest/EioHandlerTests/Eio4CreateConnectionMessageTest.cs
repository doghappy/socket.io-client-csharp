using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EioHandler;
using System.Collections.Generic;

namespace SocketIOClient.UnitTest.EioHandlerTests
{
    [TestClass]
    public class Eio4CreateConnectionMessageTest
    {
        [TestMethod]
        public void NullNamespaceNullQuery()
        {
            var handler = new Eio4Handler();
            string message = handler.CreateConnectionMessage(null, null);
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void NullNamespaceEmptyQuery()
        {
            var handler = new Eio4Handler();
            string message = handler.CreateConnectionMessage(null, new Dictionary<string, string>());
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void NullNamespace1Query()
        {
            var handler = new Eio4Handler();
            string message = handler.CreateConnectionMessage(null, new Dictionary<string, string>
            {
                { "key", "val" }
            });
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void Namespace2Query()
        {
            var handler = new Eio4Handler();
            string message = handler.CreateConnectionMessage("/nsp", new Dictionary<string, string>
            {
                { "key", "val" },
                { "token", "V2" }
            });
            Assert.AreEqual("40/nsp,", message);
        }
    }
}
