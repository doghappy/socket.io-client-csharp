using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EioHandler;

namespace SocketIOClient.UnitTest.EioHandlerTests
{
    [TestClass]
    public class Eio4CheckConnectionTest
    {
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
