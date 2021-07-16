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
