using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Packgers;

//[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]
//[assembly: DoNotParallelize]
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
            try
            {
                manager.Unpack(msg);
            }
            catch (Exception) { }
            Assert.AreEqual("mzxOPB0FoNcYh4IKAAAG", socket.Id);
        }

        [TestMethod]
        public void MessageBinaryEventPackagerTest()
        {
            var socket = new SocketIO("http://localhost:11000");
            var packger = new MessageBinaryEventPackger();
            string msg = "1-92[\"v1/read/receive\",{\"_placeholder\":true,\"num\":0}]";
            packger.Unpack(socket,msg);
            Assert.AreEqual(92, packger.Response.PacketId);
        }
    }
}
