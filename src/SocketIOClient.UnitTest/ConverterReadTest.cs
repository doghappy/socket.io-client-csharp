using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest
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
            Assert.AreEqual(CvtMessageType.MessageEvent, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(1, elements.Count);
            Assert.AreEqual("hi", elements[0].GetString());
        }

        [TestMethod]
        public void Event1Param()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "42[\"hi\",\"V3: onAny\"]");
            Assert.AreEqual(CvtMessageType.MessageEvent, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(2, elements.Count);
            Assert.AreEqual("hi", elements[0].GetString());
            Assert.AreEqual("V3: onAny", elements[1].GetString());
        }

        [TestMethod]
        public void NamespaceEvent0Param()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "42/nsp,[\"234\"]");
            Assert.AreEqual(CvtMessageType.MessageEvent, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.AreEqual("/nsp", realMsg.Namespace);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(1, elements.Count);
            Assert.AreEqual("234", elements[0].GetString());
        }

        [TestMethod]
        public void NamespaceEvent1Param()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "42/nsp,[\"qww\",true]");
            Assert.AreEqual(CvtMessageType.MessageEvent, msg.Type);

            var realMsg = msg as EventMessage;

            Assert.AreEqual("/nsp", realMsg.Namespace);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(2, elements.Count);
            Assert.AreEqual("qww", elements[0].GetString());
            Assert.IsTrue(elements[1].GetBoolean());
        }

        [TestMethod]
        public void Ack()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "431[\"doghappy\"]");
            Assert.AreEqual(CvtMessageType.MessageAck, msg.Type);

            var realMsg = msg as AckMessage;

            Assert.IsTrue(string.IsNullOrEmpty(realMsg.Namespace));
            Assert.AreEqual(1, realMsg.Id);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(1, elements.Count);
            Assert.AreEqual("doghappy", elements[0].GetString());
        }

        [TestMethod]
        public void NamespaceAck()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "43/google,15[\"doghappy\"]");
            Assert.AreEqual(CvtMessageType.MessageAck, msg.Type);

            var realMsg = msg as AckMessage;

            Assert.AreEqual("/google", realMsg.Namespace);
            Assert.AreEqual(15, realMsg.Id);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(1, elements.Count);
            Assert.AreEqual("doghappy", elements[0].GetString());
        }

        [TestMethod]
        public void Eio3Error()
        {
            var msg = CvtFactory.GetMessage(3, "44\"Authentication error\"");
            Assert.AreEqual(CvtMessageType.MessageError, msg.Type);

            var result = msg as Eio3ErrorMessage;

            Assert.AreEqual("Authentication error", result.Message);
        }

        [TestMethod]
        public void Eio4Error()
        {
            var msg = CvtFactory.GetMessage(4, "44{\"message\":\"Authentication error2\"}");
            Assert.AreEqual(CvtMessageType.MessageError, msg.Type);

            var result = msg as Eio4ErrorMessage;

            Assert.AreEqual("Authentication error2", result.Message);
        }

        [TestMethod]
        public void Binary()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "451-[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.MessageBinary, msg.Type);

            var realMsg = msg as BinaryMessage;

            Assert.AreEqual(string.Empty, realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(2, elements.Count);
            Assert.AreEqual("1 params", elements[0].GetString());
            Assert.AreEqual(JsonValueKind.Object, elements[1].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinary()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "451-/why-ve,[\"1 params\",{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.MessageBinary, msg.Type);

            var realMsg = msg as BinaryMessage;

            Assert.AreEqual("/why-ve", realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(2, elements.Count);
            Assert.AreEqual("1 params", elements[0].GetString());
            Assert.AreEqual(JsonValueKind.Object, elements[1].ValueKind);
        }

        [TestMethod]
        public void BinaryAck()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "461-6[{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.MessageBinaryAck, msg.Type);

            var realMsg = msg as BinaryAckMessage;

            Assert.IsNull(realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);
            Assert.AreEqual(6, realMsg.Id);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(1, elements.Count);
            Assert.AreEqual(JsonValueKind.Object, elements[0].ValueKind);
        }

        [TestMethod]
        public void NamespaceBinaryAck()
        {
            var msg = CvtFactory.GetMessage(It.IsAny<int>(), "461-/name-space,6[{\"_placeholder\":true,\"num\":0}]");
            Assert.AreEqual(CvtMessageType.MessageBinaryAck, msg.Type);

            var realMsg = msg as BinaryAckMessage;

            Assert.AreEqual("/name-space", realMsg.Namespace);
            Assert.AreEqual(1, realMsg.BinaryCount);
            Assert.AreEqual(6, realMsg.Id);

            var elements = realMsg.Json.EnumerateArray().ToList();
            Assert.AreEqual(1, elements.Count);
            Assert.AreEqual(JsonValueKind.Object, elements[0].ValueKind);
        }
    }
}
