using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EioHandler;
using System.Collections.Generic;

namespace SocketIOClient.UnitTest.EioHandlerTests
{
    [TestClass]
    public class CreateConnectionMessageTest
    {
        [TestMethod]
        public void Eio3NullNamespaceNullQuery()
        {
            var handler = new Eio3Handler();
            string message = handler.CreateConnectionMessage(null, null);
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void Eio3NullNamespaceEmptyQuery()
        {
            var handler = new Eio3Handler();
            string message = handler.CreateConnectionMessage(null, new Dictionary<string, string>());
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void Eio3NullNamespace1Query()
        {
            var handler = new Eio3Handler();
            string message = handler.CreateConnectionMessage(null, new Dictionary<string, string>
            {
                { "key", "val" }
            });
            Assert.AreEqual("40?key=val", message);
        }

        [TestMethod]
        public void Eio3Namespace2Query()
        {
            var handler = new Eio3Handler();
            string message = handler.CreateConnectionMessage("/nsp", new Dictionary<string, string>
            {
                { "key", "val" },
                { "token", "V2" }
            });
            Assert.AreEqual("40/nsp?key=val&token=V2,", message);
        }

        [TestMethod]
        public void Eio4NullNamespaceNullQuery()
        {
            var handler = new Eio4Handler();
            string message = handler.CreateConnectionMessage(null, null);
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void Eio4NullNamespaceEmptyQuery()
        {
            var handler = new Eio4Handler();
            string message = handler.CreateConnectionMessage(null, new Dictionary<string, string>());
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void Eio4NullNamespace1Query()
        {
            var handler = new Eio4Handler();
            string message = handler.CreateConnectionMessage(null, new Dictionary<string, string>
            {
                { "key", "val" }
            });
            Assert.AreEqual("40", message);
        }

        [TestMethod]
        public void Eio4Namespace2Query()
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
