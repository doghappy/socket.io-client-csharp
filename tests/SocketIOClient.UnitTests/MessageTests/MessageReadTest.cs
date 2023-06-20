using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.JsonSerializer;
using SocketIOClient.Messages;

namespace SocketIOClient.UnitTests.MessageTests
{
    [TestClass]
    public class MessageReadTest
    {
        [TestMethod]
        public void Ping()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4,new SystemTextJsonSerializer(), "2");
            Assert.AreEqual(MessageType.Ping, msg.Type);
        }

        [TestMethod]
        public void Pong()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "3");
            Assert.AreEqual(MessageType.Pong, msg.Type);
        }

        [TestMethod]
        public void Eio4Connected()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");
            Assert.AreEqual(MessageType.Connected, msg.Type);

            var connectedMsg = msg as ConnectedMessage<JsonElement>;

            Assert.AreEqual(string.Empty, connectedMsg.Namespace);
            Assert.AreEqual("aMA_EmVTuzpgR16PAc4w", connectedMsg.Sid);
        }

        [TestMethod]
        public void Eio4NamespaceConnected()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "40/nsp,{\"sid\":\"xO_jp2_xrGtXUveLAc4y\"}");
            Assert.AreEqual(MessageType.Connected, msg.Type);

            var connectedMsg = msg as ConnectedMessage<JsonElement>;

            Assert.AreEqual("/nsp", connectedMsg.Namespace);
            Assert.AreEqual("xO_jp2_xrGtXUveLAc4y", connectedMsg.Sid);
        }

        [TestMethod]
        public void Eio3Connected()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V3, new SystemTextJsonSerializer(), "40");
            Assert.AreEqual(MessageType.Connected, msg.Type);

            var connectedMsg = msg as ConnectedMessage<JsonElement>;

            Assert.IsTrue(string.IsNullOrEmpty(connectedMsg.Namespace));
            Assert.IsNull(connectedMsg.Sid);
        }

        [TestMethod]
        public void Eio3NamespaceConnected1()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V3, new SystemTextJsonSerializer(),"40/nsp,");
            Assert.AreEqual(MessageType.Connected, msg.Type);

            var connectedMsg = msg as ConnectedMessage<JsonElement>;

            Assert.AreEqual("/nsp", connectedMsg.Namespace);
            Assert.IsNull(connectedMsg.Sid);
        }

        [TestMethod]
        public void Eio3NamespaceConnected2()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V3, new SystemTextJsonSerializer(), "40/nsp");
            Assert.AreEqual(MessageType.Connected, msg.Type);

            var connectedMsg = msg as ConnectedMessage<JsonElement>;

            Assert.AreEqual("/nsp", connectedMsg.Namespace);
            Assert.IsNull(connectedMsg.Sid);
        }

        [TestMethod]
        public void Eio3NamespaceConnected3()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V3, new SystemTextJsonSerializer(), "40/nsp?token=V2,");
            Assert.AreEqual(MessageType.Connected, msg.Type);

            var connectedMsg = msg as ConnectedMessage<JsonElement>;

            Assert.AreEqual("/nsp", connectedMsg.Namespace);
            Assert.IsNull(connectedMsg.Sid);
        }

        [TestMethod]
        public void Eio3NamespaceConnected4()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V3, new SystemTextJsonSerializer(), "40/nsp?token=V2");
            Assert.AreEqual(MessageType.Connected, msg.Type);

            var connectedMsg = msg as ConnectedMessage<JsonElement>;

            Assert.AreEqual("/nsp", connectedMsg.Namespace);
            Assert.IsNull(connectedMsg.Sid);
        }

        [TestMethod]
        public void Disconnected()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "41");
            Assert.AreEqual(MessageType.Disconnected, msg.Type);

            var realMsg = msg as DisconnectedMessage<JsonElement>;

            Assert.AreEqual(string.Empty, realMsg.Namespace);
        }

        [TestMethod]
        public void NamespaceDisconnected()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "41/github,");
            Assert.AreEqual(MessageType.Disconnected, msg.Type);

            var realMsg = msg as DisconnectedMessage<JsonElement>;

            Assert.AreEqual("/github", realMsg.Namespace);
        }

        [TestMethod]
        public void Event0Param()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "42[\"hi\"]");
            Assert.AreEqual(MessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage<JsonElement>;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));

            Assert.AreEqual("hi", realMsg.Event);
            Assert.AreEqual(0, realMsg.JsonElements.Count);
        }

        [TestMethod]
        public void Event1Param()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "42[\"hi\",\"V3: onAny\"]");
            Assert.AreEqual(MessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage<JsonElement>;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));

            Assert.AreEqual("hi", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual("V3: onAny", realMsg.JsonElements[0].GetString());
        }

        [TestMethod]
        public void NamespaceEvent0Param()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "42/nsp,[\"234\"]");
            Assert.AreEqual(MessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage<JsonElement>;

            Assert.AreEqual("/nsp", realMsg.Namespace);

            Assert.AreEqual("234", realMsg.Event);
            Assert.AreEqual(0, realMsg.JsonElements.Count);
        }

        [TestMethod]
        public void NamespaceEvent1Param()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "42/nsp,[\"qww\",true]");
            Assert.AreEqual(MessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage<JsonElement>;

            Assert.AreEqual("/nsp", realMsg.Namespace);

            Assert.AreEqual("qww", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.IsTrue(realMsg.JsonElements[0].GetBoolean());
        }

        [TestMethod]
        public void EventMessageWithId()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "42/nsp,17[\"client calls the server's callback 0\"]");
            Assert.AreEqual(MessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage<JsonElement>;

            Assert.AreEqual("/nsp", realMsg.Namespace);

            Assert.AreEqual("client calls the server's callback 0", realMsg.Event);
            Assert.AreEqual(0, realMsg.JsonElements.Count);
            Assert.AreEqual(17, realMsg.Id);
        }

        [TestMethod]
        public void Ack()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "431[\"doghappy\"]");
            Assert.AreEqual(MessageType.AckMessage, msg.Type);

            var realMsg = msg as ClientAckMessage<JsonElement>;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));
            Assert.AreEqual(1, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual("doghappy", realMsg.JsonElements[0].GetString());
        }

        [TestMethod]
        public void NamespaceAck()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "43/google,15[\"doghappy\"]");
            Assert.AreEqual(MessageType.AckMessage, msg.Type);

            var realMsg = msg as ClientAckMessage<JsonElement>;

            Assert.AreEqual("/google", realMsg.Namespace);
            Assert.AreEqual(15, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual("doghappy", realMsg.JsonElements[0].GetString());
        }

        [TestMethod]
        public void Error()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "44{\"message\":\"Authentication error2\"}");
            Assert.AreEqual(MessageType.ErrorMessage, msg.Type);

            var result = msg as ErrorMessage<JsonElement>;

            Assert.IsNull(result.Namespace);
            Assert.AreEqual("Authentication error2", result.Message);
        }

        [TestMethod]
        public void NamespaceError()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "44/message,{\"message\":\"Authentication error\"}");
            Assert.AreEqual(MessageType.ErrorMessage, msg.Type);

            var result = msg as ErrorMessage<JsonElement>;

            Assert.AreEqual("/message", result.Namespace);
            Assert.AreEqual("Authentication error", result.Message);
        }

        [TestMethod]
        public void Binary()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(MessageType.BinaryMessage, msg.Type);

            var realMsg = msg as BinaryMessage<JsonElement>;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));
            Assert.AreEqual(1, realMsg.BinaryCount);

            Assert.AreEqual("1 params", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinary()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "451-/why-ve,[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(MessageType.BinaryMessage, msg.Type);

            var realMsg = msg as BinaryMessage<JsonElement>;

            Assert.AreEqual("/why-ve", realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);

            Assert.AreEqual("1 params", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinaryWithId()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "451-/why-ve,30[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(MessageType.BinaryMessage, msg.Type);

            var realMsg = msg as BinaryMessage<JsonElement>;

            Assert.AreEqual("/why-ve", realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);
            Assert.AreEqual(30, realMsg.Id);

            Assert.AreEqual("1 params", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }

        [TestMethod]
        public void BinaryAck()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "461-6[{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(MessageType.BinaryAckMessage, msg.Type);

            var realMsg = msg as ClientBinaryAckMessage<JsonElement>;

            Assert.IsNull(realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);
            Assert.AreEqual(6, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinaryAck()
        {
            var msg = MessageFactory<JsonElement>.CreateMessage(EngineIO.V4, new SystemTextJsonSerializer(), "461-/name-space,6[{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(MessageType.BinaryAckMessage, msg.Type);

            var realMsg = msg as ClientBinaryAckMessage<JsonElement>;

            Assert.AreEqual("/name-space", realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);
            Assert.AreEqual(6, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }
    }
}
