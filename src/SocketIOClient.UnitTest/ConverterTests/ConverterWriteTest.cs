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
    public class ConverterWriteTest
    {
        [TestMethod]
        public void Ping()
        {
            var msg = new PingMessage();
            string text = msg.Write();
            Assert.AreEqual("2", text);
        }

        [TestMethod]
        public void Pong()
        {
            var msg = new PongMessage();
            string text = msg.Write();
            Assert.AreEqual("3", text);
        }

        [TestMethod]
        public void Eio3Connected()
        {
            var msg = new Eio3ConnectedMessage
            {
                QueryString = "a=1&b=2"
            };
            string text = msg.Write();
            Assert.AreEqual("40?a=1&b=2", text);
        }

        [TestMethod]
        public void Eio3NamespaceConnected()
        {
            var msg = new Eio3ConnectedMessage
            {
                QueryString = "?token=123",
                Namespace = "/nginx"
            };
            string text = msg.Write();
            Assert.AreEqual("40/nginx?token=123", text);
        }

        [TestMethod]
        public void Eio4Connected()
        {
            var msg = new Eio4ConnectedMessage();
            string text = msg.Write();
            Assert.AreEqual("40", text);
        }

        [TestMethod]
        public void Eio4NamespaceConnected()
        {
            var msg = new Eio4ConnectedMessage
            {
                Namespace = "/microsoft"
            };
            string text = msg.Write();
            Assert.AreEqual("40/microsoft,", text);
        }

        [TestMethod]
        public void Disconnected()
        {
            var msg = new DisconnectedMessage();
            string text = msg.Write();
            Assert.AreEqual("41", text);
        }

        [TestMethod]
        public void NamespaceDisconnected()
        {
            var msg = new DisconnectedMessage
            {
                Namespace = "/hello-world"
            };
            string text = msg.Write();
            Assert.AreEqual("41/hello-world,", text);
        }

        [TestMethod]
        public void Event0Param()
        {
            var msg = new EventMessage
            {
                Json = JsonDocument.Parse("[\"event name\"]").RootElement
            };
            string text = msg.Write();
            Assert.AreEqual("42[\"event name\"]", text);
        }

        [TestMethod]
        public void Event1Param()
        {
            var msg = new EventMessage
            {
                Json = JsonDocument.Parse("[\"event name\",\"socket.io\"]").RootElement
            };
            string text = msg.Write();
            Assert.AreEqual("42[\"event name\",\"socket.io\"]", text);
        }

        [TestMethod]
        public void NamespaceEvent0Param()
        {
            var msg = new EventMessage
            {
                Json = JsonDocument.Parse("[\"event name\"]").RootElement,
                Namespace = "/test"
            };
            string text = msg.Write();
            Assert.AreEqual("42/test,[\"event name\"]", text);
        }

        [TestMethod]
        public void NamespaceEvent1Param()
        {
            var msg = new EventMessage
            {
                Json = JsonDocument.Parse("[\"event name\",1234]").RootElement,
                Namespace = "/test"
            };
            string text = msg.Write();
            Assert.AreEqual("42/test,[\"event name\",1234]", text);
        }

        [TestMethod]
        public void Ack()
        {
            var msg = new AckMessage
            {
                Json = JsonDocument.Parse("[\"event name\",1989]").RootElement,
                Id = 8964
            };
            string text = msg.Write();
            Assert.AreEqual("428964[\"event name\",1989]", text);
        }

        [TestMethod]
        public void NamespaceAck()
        {
            var msg = new AckMessage
            {
                Json = JsonDocument.Parse("[\"event name\",1989]").RootElement,
                Id = 8964,
                Namespace = "/google"
            };
            string text = msg.Write();
            Assert.AreEqual("428964/google,[\"event name\",1989]", text);
        }

        [TestMethod]
        public void Binary()
        {
            var msg = new BinaryMessage
            {
                Json = JsonDocument.Parse("[\"event name\",1989]").RootElement,
                BinaryCount = 2
            };
            string text = msg.Write();
            Assert.AreEqual("452-[\"event name\",1989]", text);
        }

        [TestMethod]
        public void NamespaceBinary()
        {
            var msg = new BinaryMessage
            {
                Json = JsonDocument.Parse("[\"event name\",1989]").RootElement,
                BinaryCount = 2,
                Namespace = "/happy"
            };
            string text = msg.Write();
            Assert.AreEqual("452-/happy,[\"event name\",1989]", text);
        }

        [TestMethod]
        public void BinaryAck()
        {
            var msg = new BinaryAckMessage
            {
                Json = JsonDocument.Parse("[\"event name\",1989]").RootElement,
                BinaryCount = 6,
                Id = 185
            };
            string text = msg.Write();
            Assert.AreEqual("456-185[\"event name\",1989]", text);
        }

        [TestMethod]
        public void NamespaceBinaryAck()
        {
            var msg = new BinaryAckMessage
            {
                Json = JsonDocument.Parse("[\"event name\",1989]").RootElement,
                BinaryCount = 6,
                Id = 185,
                Namespace = "/namespace"
            };
            string text = msg.Write();
            Assert.AreEqual("456-/namespace,185[\"event name\",1989]", text);
        }
    }
}
