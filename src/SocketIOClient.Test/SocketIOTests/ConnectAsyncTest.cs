using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests
{
    [TestClass]
    public class ConnectAsyncTest
    {
        public const string URL = "http://localhost:11000";
        public const string NSP_URL = "http://localhost:11000/nsp";

        [TestMethod]
        public async Task TimeoutTest()
        {
            var client = new SocketIO(URL, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "token", "io" }
                },
                ConnectionTimeout = TimeSpan.FromMilliseconds(10)
            });

            bool isTimeout = false;

            try
            {
                await client.ConnectAsync();
            }
            catch (TimeoutException)
            {
                isTimeout = true;
            }

            Assert.IsTrue(isTimeout);
        }
    }
}
