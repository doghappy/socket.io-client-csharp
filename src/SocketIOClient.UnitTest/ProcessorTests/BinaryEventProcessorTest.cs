using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Processors;
using System.Collections.Generic;
using System.Text.Json;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class BinaryEventProcessorTest
    {
        [TestMethod]
        public void NullNamespace2Elements()
        {
            var processor = new BinaryEventProcessor();
            int packetId = 0;
            int totalCount = 0;
            string eventName = null;
            List<JsonElement> array = new();
            processor.Process(new MessageContext
            {
                Message = "2-89[\"2 params\",{\"_placeholder\":true,\"num\":0},{\"code\":64,\"msg\":{\"_placeholder\":true,\"num\":1}}]",
                BinaryReceivedHandler = (id, count, name, arr) =>
                {
                    packetId = id;
                    array = arr;
                    totalCount = count;
                    eventName = name;
                }
            });
            Assert.AreEqual(89, packetId);
            Assert.AreEqual(2, totalCount);
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(0, array[0].GetProperty("num").GetInt32());
            Assert.AreEqual(64, array[1].GetProperty("code").GetInt32());
            Assert.AreEqual("2 params", eventName);
        }


        [TestMethod]
        public void Namespace2Elements()
        {
            var processor = new BinaryEventProcessor();
            int packetId = 0;
            int totalCount = 0;
            List<JsonElement> array = new();
            string eventName = null;
            processor.Process(new MessageContext
            {
                Namespace = "/why",
                Message = "2-/why,89[\"2 params\",{\"_placeholder\":true,\"num\":0},{\"code\":64,\"msg\":{\"_placeholder\":true,\"num\":1}}]",
                BinaryReceivedHandler = (id, count, name, arr) =>
                {
                    packetId = id;
                    array = arr;
                    totalCount = count;
                    eventName = name;
                }
            });
            Assert.AreEqual(89, packetId);
            Assert.AreEqual(2, totalCount);
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(0, array[0].GetProperty("num").GetInt32());
            Assert.AreEqual(64, array[1].GetProperty("code").GetInt32());
            Assert.AreEqual("2 params", eventName);
        }
    }
}
