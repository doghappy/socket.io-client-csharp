using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Packgers;

namespace SocketIOClient.Test
{
    [TestClass]
    public class PackgeManagerTest
    {
        [TestMethod]
        public void OpenedPackgerTest()
        {
            var socket = new SocketIO("http://localhost:11000");
            var manager = new PackgeManager(socket);
            string msg = "0{\"sid\":\"mzxOPB0FoNcYh4IKAAAG\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}";
            manager.Unpack(msg);
            Assert.AreEqual("mzxOPB0FoNcYh4IKAAAG", socket.Id);
        }
    }
}
