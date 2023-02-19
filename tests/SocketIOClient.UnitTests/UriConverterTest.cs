using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTests
{
    [TestClass]
    public class UriConverterTest
    {
        [TestMethod]
        public void GetHandshakeUriWithHttp()
        {
            var serverUri = new Uri("http://localhost");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = UriConverter.GetServerUri(false, serverUri, EngineIO.V4, string.Empty, kvs);
            Assert.AreEqual("http://localhost/socket.io/?EIO=4&transport=polling&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithHttp80()
        {
            var serverUri = new Uri("http://localhost:80");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = UriConverter.GetServerUri(false, serverUri, EngineIO.V4, string.Empty, kvs);
            Assert.AreEqual("http://localhost/socket.io/?EIO=4&transport=polling&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithHttps443()
        {
            var serverUri = new Uri("https://localhost:443");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = UriConverter.GetServerUri(false, serverUri, EngineIO.V4, "/sio", kvs);
            Assert.AreEqual("https://localhost/sio/?EIO=4&transport=polling&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithWs80()
        {
            var serverUri = new Uri("ws://localhost:80");
            var kvs = new List<KeyValuePair<string, string>>();
            var result = UriConverter.GetServerUri(true, serverUri, EngineIO.V4, string.Empty, kvs);
            Assert.AreEqual("ws://localhost/socket.io/?EIO=4&transport=websocket", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithWss443()
        {
            var serverUri = new Uri("wss://localhost:443");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = UriConverter.GetServerUri(true, serverUri, EngineIO.V4, string.Empty, kvs);

            Assert.AreEqual("wss://localhost/socket.io/?EIO=4&transport=websocket&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithHttps80()
        {
            var serverUri = new Uri("https://localhost:80");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = UriConverter.GetServerUri(false, serverUri, EngineIO.V4, string.Empty, kvs);
            Assert.AreEqual("https://localhost:80/socket.io/?EIO=4&transport=polling&token=test", result.ToString());
        }
    }
}
