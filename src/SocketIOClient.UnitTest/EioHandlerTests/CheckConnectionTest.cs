using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EioHandler;

namespace SocketIOClient.UnitTest.EioHandlerTests
{
    [TestClass]
    public class CheckConnectionTest
    {
        [TestMethod]
        public void NullNamespaceNullText()
        {
            var handler = new Eio3Handler();
            var result = handler.CheckConnection(null, null);
            Assert.IsFalse(result.Result);
            Assert.IsNull(result.Id);
        }

        [TestMethod]
        public void NullNamespaceEmptyText()
        {
            var handler = new Eio3Handler();
            var result = handler.CheckConnection(null, string.Empty);
            Assert.IsTrue(result.Result);
            Assert.IsNull(result.Id);
        }

        [TestMethod]
        public void WrongNamespace()
        {
            var handler = new Eio3Handler();
            var result = handler.CheckConnection("/test", "/nsp,");
            Assert.IsFalse(result.Result);
            Assert.IsNull(result.Id);
        }

        [TestMethod]
        public void CorrectNamespaceTextEndsWithComma()
        {
            var handler = new Eio3Handler();
            var result = handler.CheckConnection("/test", "/test,");
            Assert.IsTrue(result.Result);
            Assert.IsNull(result.Id);
        }

        [TestMethod]
        public void CorrectNamespaceTextEndsWithoutComma()
        {
            var handler = new Eio3Handler();
            var result = handler.CheckConnection("/test", "/test");
            Assert.IsTrue(result.Result);
            Assert.IsNull(result.Id);
        }

        [TestMethod]
        public void CorrectNamespaceTextContainsSid()
        {
            var handler = new Eio4Handler();
            var result = handler.CheckConnection("/test", "/test,{\"sid\":\"xO_jp2_xrGtXUveLAc4y\"}");
            Assert.IsTrue(result.Result);
            Assert.AreEqual("xO_jp2_xrGtXUveLAc4y", result.Id);
        }
    }
}
