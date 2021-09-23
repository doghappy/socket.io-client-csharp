using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest
{
    [TestClass]
    public class UriConverterTest
    {
        [TestMethod]
        public void GetHandshakeUriWithHttp()
        {
            var cvt = new UriConverter();

            var serverUri = new Uri("http://localhost");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = cvt.GetHandshakeUri(serverUri, 4, string.Empty, kvs);
            Assert.AreEqual("http://localhost/socket.io/?EIO=4&transport=polling&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithHttp80()
        {
            var cvt = new UriConverter();

            var serverUri = new Uri("http://localhost:80");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = cvt.GetHandshakeUri(serverUri, 4, string.Empty, kvs);
            Assert.AreEqual("http://localhost/socket.io/?EIO=4&transport=polling&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithHttps443()
        {
            var cvt = new UriConverter();

            var serverUri = new Uri("https://localhost:443");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = cvt.GetHandshakeUri(serverUri, 4, "/sio", kvs);
            Assert.AreEqual("https://localhost/sio/?EIO=4&transport=polling&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithWs80()
        {
            var cvt = new UriConverter();

            var serverUri = new Uri("ws://localhost:80");
            var kvs = new List<KeyValuePair<string, string>>();
            var result = cvt.GetHandshakeUri(serverUri, 4, string.Empty, kvs);
            Assert.AreEqual("http://localhost/socket.io/?EIO=4&transport=polling", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithWss443()
        {
            var cvt = new UriConverter();

            var serverUri = new Uri("wss://localhost:443");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = cvt.GetHandshakeUri(serverUri, 4, string.Empty, kvs);
            Assert.AreEqual("https://localhost/socket.io/?EIO=4&transport=polling&token=test", result.ToString());
        }

        [TestMethod]
        public void GetHandshakeUriWithHttps80()
        {
            var cvt = new UriConverter();

            var serverUri = new Uri("https://localhost:80");
            var kvs = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("token", "test")
            };
            var result = cvt.GetHandshakeUri(serverUri, 4, string.Empty, kvs);
            Assert.AreEqual("https://localhost:80/socket.io/?EIO=4&transport=polling&token=test", result.ToString());
        }
    }
}
