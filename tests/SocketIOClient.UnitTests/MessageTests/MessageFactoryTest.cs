using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;
using System.Text.Json;

namespace SocketIOClient.UnitTests.MessageTests
{
    [TestClass]
    public class MessageFactoryTest
    {
        [TestMethod]
        public void CreateEio3HttpOpenedMessage()
        {
            var msg = MessageFactory<JsonElement>.CreateOpenedMessage("97:0{\"sid\":\"wOuAvDB9Jj6yE0VrAL8N\",\"upgrades\":[\"websocket\"],\"pingInterval\":25000,\"pingTimeout\":30000}", new SystemTextJsonSerializer());
            Assert.AreEqual(MessageType.Opened, msg.Type);
            Assert.AreEqual("wOuAvDB9Jj6yE0VrAL8N", msg.Sid);
            Assert.AreEqual(25000, msg.PingInterval);
            Assert.AreEqual(30000, msg.PingTimeout);
            Assert.AreEqual(EngineIO.V3, msg.EIO);
            Assert.AreEqual(1, msg.Upgrades.Count);
            Assert.AreEqual("websocket", msg.Upgrades[0]);
        }

        [TestMethod]
        public void CreateEio3HttpOpenedMessageWithQuote()
        {
            var msg = MessageFactory<JsonElement>.CreateOpenedMessage("97:0{\"sid\":\"wOuAvDB9Jj6yE0VrAL8N\",\"upgrades\":[\"websocket\"],\"pingInterval\":\"26000\",\"pingTimeout\":\"31000\"}", new SystemTextJsonSerializer());
            Assert.AreEqual(MessageType.Opened, msg.Type);
            Assert.AreEqual("wOuAvDB9Jj6yE0VrAL8N", msg.Sid);
            Assert.AreEqual(26000, msg.PingInterval);
            Assert.AreEqual(31000, msg.PingTimeout);
            Assert.AreEqual(EngineIO.V3, msg.EIO);
            Assert.AreEqual(1, msg.Upgrades.Count);
            Assert.AreEqual("websocket", msg.Upgrades[0]);
        }

        [TestMethod]
        public void CreateEio4OpenedMessage()
        {
            var msg = MessageFactory<JsonElement>.CreateOpenedMessage("0{\"sid\":\"6lV4Ef7YOyGF-5dCBvKy\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}", new SystemTextJsonSerializer());
            Assert.AreEqual(MessageType.Opened, msg.Type);
            Assert.AreEqual("6lV4Ef7YOyGF-5dCBvKy", msg.Sid);
            Assert.AreEqual(10000, msg.PingInterval);
            Assert.AreEqual(5000, msg.PingTimeout);
            Assert.AreEqual(EngineIO.V4, msg.EIO);
            Assert.AreEqual(0, msg.Upgrades.Count);
        }

        [TestMethod]
        public void CreateEio4OpenedMessageWithQuote()
        {
            var msg = MessageFactory<JsonElement>.CreateOpenedMessage("0{\"sid\":\"6lV4Ef7YOyGF-5dCBvKy\",\"upgrades\":[],\"pingInterval\":\"20000\",\"pingTimeout\":\"6000\"}", new SystemTextJsonSerializer());
            Assert.AreEqual(MessageType.Opened, msg.Type);
            Assert.AreEqual("6lV4Ef7YOyGF-5dCBvKy", msg.Sid);
            Assert.AreEqual(20000, msg.PingInterval);
            Assert.AreEqual(6000, msg.PingTimeout);
            Assert.AreEqual(EngineIO.V4, msg.EIO);
            Assert.AreEqual(0, msg.Upgrades.Count);
        }
    }
}
