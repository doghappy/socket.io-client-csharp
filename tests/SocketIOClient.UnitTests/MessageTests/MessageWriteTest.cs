using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Messages;
using SocketIOClient.Transport;
using System;
using System.Collections.Generic;

namespace SocketIOClient.UnitTests.MessageTests
{
    [TestClass]
    public class MessageWriteTest
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
        public void Eio4Connected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V4
            };
            string text = msg.Write();
            Assert.AreEqual("40", text);
        }

        [TestMethod]
        public void Eio4NamespaceConnected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V4,
                Namespace = "/microsoft"
            };
            string text = msg.Write();
            Assert.AreEqual("40/microsoft,", text);
        }

        [TestMethod]
        public void Eio3WsWithoutQueryConnected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V3,
                Protocol = TransportProtocol.WebSocket,
                Namespace = "/admin"
            };
            string text = msg.Write();
            Assert.AreEqual("40/admin,", text);
        }

        [TestMethod]
        public void Eio3WsWith1ParamConnected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V3,
                Protocol = TransportProtocol.WebSocket,
                Namespace = "/apple",
                Query = new Dictionary<string,string>
                {
                    { "a", "123" }
                }
            };
            string text = msg.Write();
            Assert.AreEqual("40/apple?a=123,", text);
        }

        [TestMethod]
        public void Eio3WsWith2ParamConnected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V3,
                Protocol = TransportProtocol.WebSocket,
                Namespace = "/razer",
                Query = new Dictionary<string, string>
                {
                    { "a", "123" },
                    { "token", "qwer" }
                }
            };
            string text = msg.Write();
            Assert.AreEqual("40/razer?a=123&token=qwer,", text);
        }

        [TestMethod]
        public void Eio3PollingWithoutQueryConnected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V3,
                Protocol = TransportProtocol.Polling,
                Namespace = "/admin"
            };
            string text = msg.Write();
            Assert.AreEqual("40/admin,", text);
        }

        [TestMethod]
        public void Eio3PollingWith1ParamConnected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V3,
                Protocol = TransportProtocol.Polling,
                Namespace = "/apple",
                Query = new Dictionary<string, string>
                {
                    { "a", "123" }
                }
            };
            string text = msg.Write();
            Assert.AreEqual("40/apple?a=123,", text);
        }

        [TestMethod]
        public void Eio3PollingWith2ParamConnected()
        {
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V3,
                Protocol = TransportProtocol.Polling,
                Namespace = "/razer",
                Query = new Dictionary<string, string>
                {
                    { "a", "123" },
                    { "token", "qwer" }
                }
            };
            string text = msg.Write();
            Assert.AreEqual("40/razer?a=123&token=qwer,", text);
        }

        [TestMethod]
        public void Eio4_Auth_Query_Namespace_Should_Include_Auth_Namespace(){
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V4,
                Protocol = TransportProtocol.Polling,
                Namespace = "/razer",
                Query = new Dictionary<string, string>
                {
                    { "a", "123" },
                    { "token", "qwer" }
                },
                AuthJsonStr = "{\"test\":\"vvs\"}"
            };
            string text = msg.Write();
            Assert.AreEqual("40/razer,{\"test\":\"vvs\"}", text);
        }

        [TestMethod]
        public void Eio4_Auth_Should_Include_Auth(){
            var msg = new ConnectedMessage
            {
                EIO = EngineIO.V4,
                Protocol = TransportProtocol.Polling,
                AuthJsonStr = "{\"test\":\"vvs\"}"
            };
            string text = msg.Write();
            Assert.AreEqual("40{\"test\":\"vvs\"}", text);
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
                Event = "event name",
            };
            string text = msg.Write();
            Assert.AreEqual("42[\"event name\"]", text);
        }

        [TestMethod]
        public void Event1Param()
        {
            var msg = new EventMessage
            {
                Event = "event name",
                Json = "[\"socket.io\"]"
            };
            string text = msg.Write();
            Assert.AreEqual("42[\"event name\",\"socket.io\"]", text);
        }

        [TestMethod]
        public void NamespaceEvent0Param()
        {
            var msg = new EventMessage
            {
                Event = "event name",
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
                Event = "event name",
                Json = "[1234]",
                Namespace = "/test"
            };
            string text = msg.Write();
            Assert.AreEqual("42/test,[\"event name\",1234]", text);
        }

        [TestMethod]
        public void Ack()
        {
            var msg = new ClientAckMessage
            {
                Event = "event name",
                Json = "[1989]",
                Id = 8964
            };
            string text = msg.Write();
            Assert.AreEqual("428964[\"event name\",1989]", text);
        }

        [TestMethod]
        public void NamespaceAck()
        {
            var msg = new ClientAckMessage
            {
                Event = "event name",
                Json = "[1989]",
                Id = 8964,
                Namespace = "/google"
            };
            string text = msg.Write();
            Assert.AreEqual("42/google,8964[\"event name\",1989]", text);
        }

        [TestMethod]
        public void ServerAckWihtoutJson()
        {
            var msg = new ServerAckMessage
            {
                Id = 8964
            };
            string text = msg.Write();
            Assert.AreEqual("438964[]", text);
        }

        [TestMethod]
        public void NamespaceServerAck()
        {
            var msg = new ServerAckMessage
            {
                Json = "[1989,\"test\",false]",
                Id = 8964,
                Namespace = "/google"
            };
            string text = msg.Write();
            Assert.AreEqual("43/google,8964[1989,\"test\",false]", text);
        }

        [TestMethod]
        public void Binary()
        {
            var msg = new BinaryMessage
            {
                Event = "event name",
                Json = "[1989]",
                OutgoingBytes = new List<byte[]>
                {
                    Array.Empty<byte>(),
                    Array.Empty<byte>()
                }
            };
            string text = msg.Write();
            Assert.AreEqual("452-[\"event name\",1989]", text);
        }

        [TestMethod]
        public void NamespaceBinary()
        {
            var msg = new BinaryMessage
            {
                Event = "event name",
                Json = "[1989]",
                Namespace = "/happy",
                OutgoingBytes = new List<byte[]>
                {
                    Array.Empty<byte>(),
                    Array.Empty<byte>()
                }
            };
            string text = msg.Write();
            Assert.AreEqual("452-/happy,[\"event name\",1989]", text);
        }

        [TestMethod]
        public void BinaryAck()
        {
            var msg = new ClientBinaryAckMessage
            {
                Event = "event name",
                Json = "[1989]",
                Id = 185,
                OutgoingBytes = new List<byte[]>
                {
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>()
                }
            };
            string text = msg.Write();
            Assert.AreEqual("456-185[\"event name\",1989]", text);
        }

        [TestMethod]
        public void NamespaceBinaryAck()
        {
            var msg = new ClientBinaryAckMessage
            {
                Event = "event name",
                Json = "[1989]",
                Id = 185,
                Namespace = "/namespace",
                OutgoingBytes = new List<byte[]>
                {
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    Array.Empty<byte>()
                }
            };
            string text = msg.Write();
            Assert.AreEqual("456-/namespace,185[\"event name\",1989]", text);
        }

        [TestMethod]
        public void ServerBinaryAck()
        {
            var msg = new ServerBinaryAckMessage
            {
                Json = "[1989,\"test\",false]",
                Id = 185,
                OutgoingBytes = new List<byte[]>
                {
                    Array.Empty<byte>()
                }
            };
            string text = msg.Write();
            Assert.AreEqual("461-185[1989,\"test\",false]", text);
        }

        [TestMethod]
        public void NamespaceServerBinaryAck()
        {
            var msg = new ServerBinaryAckMessage
            {
                Id = 185,
                Namespace = "/q",
                OutgoingBytes = new List<byte[]>
                {
                    Array.Empty<byte>(),
                    Array.Empty<byte>()
                }
            };
            string text = msg.Write();
            Assert.AreEqual("462-/q,185[]", text);
        }
    }
}
