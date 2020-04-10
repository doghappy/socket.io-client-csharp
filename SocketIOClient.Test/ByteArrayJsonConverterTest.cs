using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SocketIOClient.Parsers;
using System.Text;

namespace SocketIOClient.Test
{
    [TestClass]
    public class ByteArrayJsonConverterTest
    {
        [TestMethod]
        public void SingleBufferTest()
        {
            var ctx = new ParserContext();
            var converter = new ByteArrayJsonConverter(ctx);

            var data1 = new
            {
                code = 200,
                data = Encoding.UTF8.GetBytes("1"),
                test = "Awesome:)"
            };
            string json = JsonConvert.SerializeObject(data1, converter);

            Assert.AreEqual(1, ctx.SendBuffers.Count);
            Assert.AreEqual(4, ctx.SendBuffers[0][0]);
            Assert.AreEqual(data1.data[0], ctx.SendBuffers[0][1]);
            Assert.AreEqual("{\"code\":200,\"data\":{\"_placeholder\":true,\"num\":0},\"test\":\"Awesome:)\"}", json);
        }
    }
}
