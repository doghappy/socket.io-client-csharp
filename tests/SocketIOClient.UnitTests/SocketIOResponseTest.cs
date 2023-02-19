using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using System.Text.Json;

namespace SocketIOClient.UnitTests
{
    [TestClass]
    public class SocketIOResponseTest
    {
        [TestMethod]
        [DataRow("[1]")]
        [DataRow("[true]")]
        //[DataRow("[undefined]")]
        [DataRow("[null]")]
        [DataRow("[\"hi\",\"arr\",[1,true,\"vvv\"]]")]
        public void TestToString(string json)
        {
            var array = JsonDocument.Parse(json).RootElement.EnumerateArray().ToList();
            var response = new SocketIOResponse(array, null);
            Assert.AreEqual(json, response.ToString());
        }
    }
}
