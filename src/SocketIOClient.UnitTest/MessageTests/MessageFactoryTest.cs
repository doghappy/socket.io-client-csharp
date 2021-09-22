using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Messages;

namespace SocketIOClient.UnitTest.MessageTests
{
    [TestClass]
    public class MessageFactoryTest
    {
        [TestMethod]
        public void CreateEio3OpenedMessage()
        {
            var msg = MessageFactory.CreateOpenedMessage("97:0{\"sid\":\"wOuAvDB9Jj6yE0VrAL8N\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}");
            Assert.AreEqual(MessageType.Opened, msg.Type);
            Assert.AreEqual("wOuAvDB9Jj6yE0VrAL8N", msg.Sid);
            Assert.AreEqual(25000, msg.PingInterval);
            Assert.AreEqual(30000, msg.PingTimeout);
            Assert.AreEqual(3, msg.Eio);
            Assert.AreEqual(1, msg.Upgrades.Count);
            Assert.AreEqual("websocket", msg.Upgrades[0]);
        }

        [TestMethod]
        public void CreateEio4OpenedMessage()
        {
            var msg = MessageFactory.CreateOpenedMessage("0{\"sid\":\"6lV4Ef7YOyGF-5dCBvKy\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}");
            Assert.AreEqual(MessageType.Opened, msg.Type);
            Assert.AreEqual("6lV4Ef7YOyGF-5dCBvKy", msg.Sid);
            Assert.AreEqual(10000, msg.PingInterval);
            Assert.AreEqual(5000, msg.PingTimeout);
            Assert.AreEqual(4, msg.Eio);
            Assert.AreEqual(0, msg.Upgrades.Count);
        }
    }
}
