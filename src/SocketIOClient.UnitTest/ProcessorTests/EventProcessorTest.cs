using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Processors;
using System.Collections.Generic;
using System.Text.Json;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class EventProcessorTest
    {
        [TestMethod]
        public void TestWithoutNamespace()
        {
            int packetId = 0;
            string eventName = null;
            List<JsonElement> array = null;

            var processor = new EventProcessor();
            processor.Process(new MessageContext
            {
                Message = "[\"hi\",\"vvv\"]",
                EventReceivedHandler = (id, name, arr) =>
                {
                    packetId = id;
                    eventName = name;
                    array = arr;
                }
            });

            Assert.AreEqual(0, packetId);
            Assert.AreEqual("hi", eventName);
            Assert.AreEqual(1, array.Count);
            Assert.AreEqual("vvv", array[0].GetString());
        }

        [TestMethod]
        public void TestArrayWithoutNamespace()
        {
            int packetId = 0;
            string eventName = null;
            List<JsonElement> array = null;

            var processor = new EventProcessor();
            processor.Process(new MessageContext
            {
                Message = "[\"hi\",\"arr\",[1,true,\"vvv\"]]",
                EventReceivedHandler = (id, name, arr) =>
                {
                    packetId = id;
                    eventName = name;
                    array = arr;
                }
            });

            Assert.AreEqual(0, packetId);
            Assert.AreEqual("hi", eventName);
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual("arr", array[0].GetString());
            Assert.AreEqual(JsonValueKind.Array, array[1].ValueKind);
            Assert.AreEqual(1, array[1][0].GetInt32());
            Assert.AreEqual(true, array[1][1].GetBoolean());
            Assert.AreEqual("vvv", array[1][2].GetString());
        }

        [TestMethod]
        public void TestWithNamespace()
        {
            int packetId = 0;
            string eventName = null;
            List<JsonElement> array = null;

            var processor = new EventProcessor();
            processor.Process(new MessageContext
            {
                Namespace = "/ms",
                Message = "/ms,[\"hi\",\"vvv\"]",
                EventReceivedHandler = (id, name, arr) =>
                {
                    packetId = id;
                    eventName = name;
                    array = arr;
                }
            });

            Assert.AreEqual(0, packetId);
            Assert.AreEqual("hi", eventName);
            Assert.AreEqual(1, array.Count);
            Assert.AreEqual("vvv", array[0].GetString());
        }
    }
}
