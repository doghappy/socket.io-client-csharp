using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;

namespace SocketIOClient.IntegrationTest
{
    [TestClass]
    public class UrlConverterTest
    {
        [TestMethod]
        public void HttpWithPortTest()
        {
            var urlConverter = new UrlConverter();
            Uri httpUri = new Uri("http://localhost:3000");
            Uri wsUri = urlConverter.HttpToWs(httpUri, new SocketIOOptions());

            Assert.AreEqual("ws://localhost:3000/socket.io/?EIO=4&transport=websocket", wsUri.ToString());
        }

        [TestMethod]
        public void HttpsWithPortTest()
        {
            var urlConverter = new UrlConverter();
            Uri httpUri = new Uri("https://localhost:3000");
            Uri wsUri = urlConverter.HttpToWs(httpUri, new SocketIOOptions());

            Assert.AreEqual("wss://localhost:3000/socket.io/?EIO=4&transport=websocket", wsUri.ToString());
        }

        [TestMethod]
        public void HttpWithoutPortTest()
        {
            var urlConverter = new UrlConverter();
            Uri httpUri = new Uri("http://localhost");
            Uri wsUri = urlConverter.HttpToWs(httpUri, new SocketIOOptions());

            Assert.AreEqual("ws://localhost/socket.io/?EIO=4&transport=websocket", wsUri.ToString());
        }

        [TestMethod]
        public void HttpsWithoutPortTest()
        {
            var urlConverter = new UrlConverter();
            Uri httpUri = new Uri("https://localhost");
            Uri wsUri = urlConverter.HttpToWs(httpUri, new SocketIOOptions());

            Assert.AreEqual("wss://localhost/socket.io/?EIO=4&transport=websocket", wsUri.ToString());
        }

        [TestMethod]
        public void ParametersTest()
        {
            var urlConverter = new UrlConverter();
            Uri httpUri = new Uri("https://localhost");
            Uri wsUri = urlConverter.HttpToWs(httpUri, new SocketIOOptions
            {
                Query = new Dictionary<string, string>
                {
                    { "uid", "abc" },
                    { "pwd", "123" }
                }
            });

            Assert.AreEqual("wss://localhost/socket.io/?EIO=4&transport=websocket&uid=abc&pwd=123", wsUri.ToString());
        }

        [TestMethod]
        public void CustomPathTest()
        {
            var urlConverter = new UrlConverter();
            Uri httpUri = new Uri("https://localhost");
            Uri wsUri = urlConverter.HttpToWs(httpUri, new SocketIOOptions
            {
                Path = "/test",
                Query = new Dictionary<string, string>
                {
                    { "uid", "abc" },
                    { "pwd", "123" }
                }
            });

            Assert.AreEqual("wss://localhost/test/?EIO=4&transport=websocket&uid=abc&pwd=123", wsUri.ToString());
        }

        [TestMethod]
        public void EIO3Test()
        {
            var urlConverter = new UrlConverter();
            Uri httpUri = new Uri("http://localhost:3000");
            Uri wsUri = urlConverter.HttpToWs(httpUri, new SocketIOOptions
            {
                EIO = 3
            });

            Assert.AreEqual("ws://localhost:3000/socket.io/?EIO=3&transport=websocket", wsUri.ToString());
        }
    }
}
