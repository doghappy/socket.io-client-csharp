using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.EioHandler;
using System.Text;

namespace SocketIOClient.UnitTest.EioHandlerTests
{
    [TestClass]
    public class GetBytesTest
    {
        [TestMethod]
        public void Eio3GetBytes()
        {
            var handler = new Eio3Handler();
            var bytes = Encoding.UTF8.GetBytes("hello world!");
            var output = handler.GetBytes(bytes);

            Assert.AreEqual(bytes.Length - 1, output.Length);
            Assert.AreEqual("ello world!", Encoding.UTF8.GetString(output));
        }

        [TestMethod]
        public void Eio4GetBytes()
        {
            var handler = new Eio4Handler();
            var bytes = Encoding.UTF8.GetBytes("hello world!");
            var output = handler.GetBytes(bytes);

            Assert.AreEqual(bytes.Length, output.Length);
            Assert.AreEqual("hello world!", Encoding.UTF8.GetString(output));
        }
    }
}
