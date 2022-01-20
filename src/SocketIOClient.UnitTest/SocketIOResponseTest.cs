using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace SocketIOClient.UnitTest
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
            var array = JArray.Parse(json);
            var response = new SocketIOResponse(array, null);
            Assert.AreEqual(json, response.ToString());
        }
    }
}
