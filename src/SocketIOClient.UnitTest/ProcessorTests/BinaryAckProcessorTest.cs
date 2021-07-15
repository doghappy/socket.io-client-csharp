using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Processors;
using System.Collections.Generic;
using System.Text.Json;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class BinaryAckProcessorTest
    {
        [TestMethod]
        public void NullNamespace2Elements()
        {
            var processor = new BinaryAckProcessor();
            int packetId = 0;
            int totalCount = 0;
            List<JsonElement> array = new();
            processor.Process(new MessageContext
            {
                Message = "2-89[{\"_placeholder\":true,\"num\":0},{\"code\":64,\"msg\":{\"_placeholder\":true,\"num\":1}}]",
                BinaryAckHandler = (id, count, arr) =>
                {
                    packetId = id;
                    array = arr;
                    totalCount = count;
                }
            });
            Assert.AreEqual(89, packetId);
            Assert.AreEqual(2, totalCount);
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(0, array[0].GetProperty("num").GetInt32());
            Assert.AreEqual(64, array[1].GetProperty("code").GetInt32());
        }


        [TestMethod]
        public void Namespace2Elements()
        {
            var processor = new BinaryAckProcessor();
            int packetId = 0;
            int totalCount = 0;
            List<JsonElement> array = new();
            processor.Process(new MessageContext
            {
                Namespace = "/why",
                Message = "2-/why,89[{\"_placeholder\":true,\"num\":0},{\"code\":64,\"msg\":{\"_placeholder\":true,\"num\":1}}]",
                BinaryAckHandler = (id, count, arr) =>
                {
                    packetId = id;
                    array = arr;
                    totalCount = count;
                }
            });
            Assert.AreEqual(89, packetId);
            Assert.AreEqual(2, totalCount);
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual(0, array[0].GetProperty("num").GetInt32());
            Assert.AreEqual(64, array[1].GetProperty("code").GetInt32());
        }
    }
}
