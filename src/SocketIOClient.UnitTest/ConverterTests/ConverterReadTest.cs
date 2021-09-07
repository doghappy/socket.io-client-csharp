using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.ConverterTests
{
    [TestClass]
    public class ConverterReadTest
    {
        [TestMethod]
        public void Opened()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "0{\"sid\":\"6lV4Ef7YOyGF-5dCBvKy\",\"upgrades\":[],\"pingInterval\":10000,\"pingTimeout\":5000}");
            Assert.AreEqual(CvtMessageType.Opened, msg.Type);

            var openedMsg = msg as OpenedMessage;

            Assert.AreEqual("6lV4Ef7YOyGF-5dCBvKy", openedMsg.Sid);
            Assert.AreEqual(10000, openedMsg.PingInterval);
            Assert.AreEqual(5000, openedMsg.PingTimeout);
        }

        [TestMethod]
        public void Ping()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "2");
            Assert.AreEqual(CvtMessageType.Ping, msg.Type);
        }

        [TestMethod]
        public void Pong()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "3");
            Assert.AreEqual(CvtMessageType.Pong, msg.Type);
        }

        [TestMethod]
        public void Eio3Connected()
        {
            var msg = CvtFactory.GetMessage(3, "40");
            Assert.AreEqual(CvtMessageType.Connected, msg.Type);

            var connectedMsg = msg as Eio3ConnectedMessage;

            Assert.AreEqual(string.Empty, connectedMsg.Namespace);
        }

        [TestMethod]
        public void Eio3NamespaceConnected()
        {
            var msg = CvtFactory.GetMessage(3, "40/nsp,");
            Assert.AreEqual(CvtMessageType.Connected, msg.Type);

            var connectedMsg = msg as Eio3ConnectedMessage;

            Assert.AreEqual("/nsp", connectedMsg.Namespace);
        }

        [TestMethod]
        public void Eio4Connected()
        {
            var msg = CvtFactory.GetMessage(4, "40{\"sid\":\"aMA_EmVTuzpgR16PAc4w\"}");
            Assert.AreEqual(CvtMessageType.Connected, msg.Type);

            var connectedMsg = msg as Eio4ConnectedMessage;

            Assert.AreEqual(string.Empty, connectedMsg.Namespace);
            Assert.AreEqual("aMA_EmVTuzpgR16PAc4w", connectedMsg.Sid);
        }

        [TestMethod]
        public void Eio4NamespaceConnected()
        {
            var msg = CvtFactory.GetMessage(4, "40/nsp,{\"sid\":\"xO_jp2_xrGtXUveLAc4y\"}");
            Assert.AreEqual(CvtMessageType.Connected, msg.Type);

            var connectedMsg = msg as Eio4ConnectedMessage;

            Assert.AreEqual("/nsp", connectedMsg.Namespace);
            Assert.AreEqual("xO_jp2_xrGtXUveLAc4y", connectedMsg.Sid);
        }

        [TestMethod]
        public void Disconnected()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "41");
            Assert.AreEqual(CvtMessageType.Disconnected, msg.Type);

            var realMsg = msg as DisconnectedMessage;

            Assert.AreEqual(string.Empty, realMsg.Namespace);
        }

        [TestMethod]
        public void NamespaceDisconnected()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "41/github,");
            Assert.AreEqual(CvtMessageType.Disconnected, msg.Type);

            var realMsg = msg as DisconnectedMessage;

            Assert.AreEqual("/github", realMsg.Namespace);
        }

        [TestMethod]
        public void Event0Param()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "42[\"hi\"]");
            Assert.AreEqual(CvtMessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));

            Assert.AreEqual("hi", realMsg.Event);
            Assert.AreEqual(0, realMsg.JsonElements.Count);
        }

        [TestMethod]
        public void Event1Param()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "42[\"hi\",\"V3: onAny\"]");
            Assert.AreEqual(CvtMessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));

            Assert.AreEqual("hi", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual("V3: onAny", realMsg.JsonElements[0].GetString());
        }

        [TestMethod]
        public void NamespaceEvent0Param()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "42/nsp,[\"234\"]");
            Assert.AreEqual(CvtMessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.AreEqual("/nsp", realMsg.Namespace);

            Assert.AreEqual("234", realMsg.Event);
            Assert.AreEqual(0, realMsg.JsonElements.Count);
        }

        [TestMethod]
        public void NamespaceEvent1Param()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "42/nsp,[\"qww\",true]");
            Assert.AreEqual(CvtMessageType.EventMessage, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.AreEqual("/nsp", realMsg.Namespace);

            Assert.AreEqual("qww", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.IsTrue(realMsg.JsonElements[0].GetBoolean());
        }

        [TestMethod]
        public void Ack()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "431[\"doghappy\"]");
            Assert.AreEqual(CvtMessageType.AckMessage, msg.Type);

            var realMsg = msg as ServerAckMessage;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));
            Assert.AreEqual(1, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual("doghappy", realMsg.JsonElements[0].GetString());
        }

        [TestMethod]
        public void NamespaceAck()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "43/google,15[\"doghappy\"]");
            Assert.AreEqual(CvtMessageType.AckMessage, msg.Type);

            var realMsg = msg as ServerAckMessage;

            Assert.AreEqual("/google", realMsg.Namespace);
            Assert.AreEqual(15, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual("doghappy", realMsg.JsonElements[0].GetString());
        }

        [TestMethod]
        public void Eio3Error()
        {
            var msg = CvtFactory.GetMessage(3, "44\"Authentication error\"");
            Assert.AreEqual(CvtMessageType.ErrorMessage, msg.Type);

            var result = msg as Eio3ErrorMessage;

            Assert.AreEqual("Authentication error", result.Message);
        }

        [TestMethod]
        public void Eio4Error()
        {
            var msg = CvtFactory.GetMessage(4, "44{\"message\":\"Authentication error2\"}");
            Assert.AreEqual(CvtMessageType.ErrorMessage, msg.Type);

            var result = msg as Eio4ErrorMessage;

            Assert.AreEqual("Authentication error2", result.Message);
        }

        [TestMethod]
        public void Binary()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.BinaryMessage, msg.Type);

            var realMsg = msg as BinaryMessage;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));
            Assert.AreEqual(1, realMsg.BinaryCount);

            Assert.AreEqual("1 params", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinary()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "451-/why-ve,[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.BinaryMessage, msg.Type);

            var realMsg = msg as BinaryMessage;

            Assert.AreEqual("/why-ve", realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);

            Assert.AreEqual("1 params", realMsg.Event);
            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinaryWithId()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "451-/why-ve,30[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.BinaryMessage, msg.Type);

            var realMsg = msg as BinaryMessage;

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
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "461-6[{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.BinaryAckMessage, msg.Type);

            var realMsg = msg as ServerBinaryAckMessage;

            Assert.IsNull(realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);
            Assert.AreEqual(6, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinaryAck()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "461-/name-space,6[{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.BinaryAckMessage, msg.Type);

            var realMsg = msg as ServerBinaryAckMessage;

            Assert.AreEqual("/name-space", realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);
            Assert.AreEqual(6, realMsg.Id);

            Assert.AreEqual(1, realMsg.JsonElements.Count);
            Assert.AreEqual(JsonValueKind.Object, realMsg.JsonElements[0].ValueKind);
        }
    }
}
