using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.Processors;
using System.Collections.Generic;
using System.Text.Json;

namespace SocketIOClient.UnitTest.ProcessorTests
{
    [TestClass]
    public class AckProcessorTest
    {
        [TestMethod]
        public void NullNamespace2Elements()
        {
            var processor = new AckProcessor();
            int packetId = 0;
            List<JsonElement> array = new();
            processor.Process(new MessageContext
            {
                Message = "1[\"hi\",\"onAny\"]",
                AckHandler = (id, arr) =>
                {
                    packetId = id;
                    array = arr;
                }
            });
            Assert.AreEqual(1, packetId);
            Assert.AreEqual(2, array.Count);
            Assert.AreEqual("hi", array[0].GetString());
            Assert.AreEqual("onAny", array[1].GetString());
        }


        [TestMethod]
        public void Namespace3Elements()
        {
            var processor = new AckProcessor();
            int packetId = 0;
            List<JsonElement> array = new();
            processor.Process(new MessageContext
            {
                Namespace = "/nsp",
                Message = "/nsp,2[\"hi\",\"onAny\", true]",
                AckHandler = (id, arr) =>
                {
                    packetId = id;
                    array = arr;
                }
            });
            Assert.AreEqual(2, packetId);
            Assert.AreEqual(3, array.Count);
            Assert.AreEqual("hi", array[0].GetString());
            Assert.AreEqual("onAny", array[1].GetString());
            Assert.IsTrue(array[2].GetBoolean());
        }
    }
}
