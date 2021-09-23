using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RichardSzalay.MockHttp;
using SocketIOClient.Transport;
using SocketIOClient.UriConverters;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.UnitTest.TransportTests
{
    [TestClass]
    public class HttpTransportTest
    {
        [TestMethod]
        public async Task TextWithBinaryTest()
        {
            string uri = "http://localhost:11003/socket.io/?token=V3&EIO=4&transport=polling";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond("text/plain", "452-[\"2 params\",{\"_placeholder\":true,\"num\":0},{\"code\":64,\"msg\":{\"_placeholder\":true,\"num\":1}}]bCjAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMAoxMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTEKMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyCjMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMwo0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQKNTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1CjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2Ngo3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3NzcKODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4Cjk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OQpBbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWUK5L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2gCuOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBrgphYmM=bCjAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMDAwMAoxMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTExMTEKMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyMjIyCjMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMzMwo0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQ0NDQKNTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1NTU1CjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2NjY2Ngo3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3Nzc3NzcKODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4ODg4Cjk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OTk5OQpBbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWVyaWNhbkFtZXJpY2FuQW1lcmljYW5BbWUK5L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2g5aW95L2gCuOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBruOBrgp4eXo=");
            var httpClient = mockHttp.ToHttpClient();

            var clientWebSocket = new Mock<IClientWebSocket>();

            var uriConverter = new Mock<IUriConverter>();
            uriConverter
                .Setup(x => x.GetHandshakeUri(It.IsAny<Uri>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(new Uri(uri));

            string resultText = null;
            var bytes = new List<byte[]>();
            var transport = new HttpTransport(httpClient, 4)
            {
                OnTextReceived = text => resultText = text,
                OnBinaryReceived = b => bytes.Add(b)
            };
            await transport.GetAsync(uri, CancellationToken.None);

            await Task.Delay(100);

            string longString = @"
000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222222
333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333
444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444444
555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555555
666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666666
777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777777
888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888888
999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999999
AmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAmericanAme
你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你好你
ののののののののののののののののののののののののののののののののののののののののののののののののののののののののののののの
";

            Assert.AreEqual("452-[\"2 params\",{\"_placeholder\":true,\"num\":0},{\"code\":64,\"msg\":{\"_placeholder\":true,\"num\":1}}]", resultText);
            Assert.AreEqual(2, bytes.Count);
            string str1 = longString + "abc";
            string str2 = Encoding.UTF8.GetString(bytes[0]);
            int c1 = CultureInfo.CurrentCulture.CompareInfo.Compare(str1, str2, CompareOptions.IgnoreSymbols);
            string str3 = longString + "xyz";
            string str4 = Encoding.UTF8.GetString(bytes[1]);
            int c2 = CultureInfo.CurrentCulture.CompareInfo.Compare(str1, str2, CompareOptions.IgnoreSymbols);
            Assert.AreEqual(0, c1);
            Assert.AreEqual(0, c2);
        }

        [TestMethod]
        public async Task Eio3HttpConnectedTest()
        {
            string uri = "http://localhost:11002/socket.io/?token=V3&EIO=3&transport=polling";

            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond("text/plain", "2:40");
            var httpClient = mockHttp.ToHttpClient();

            var clientWebSocket = new Mock<IClientWebSocket>();

            var uriConverter = new Mock<IUriConverter>();
            uriConverter
                .Setup(x => x.GetHandshakeUri(It.IsAny<Uri>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(new Uri(uri));

            var result = new List<string>();
            var transport = new HttpTransport(httpClient, 3)
            {
                OnTextReceived = text => result.Add(text)
            };
            await transport.GetAsync(uri, CancellationToken.None);

            await Task.Delay(100);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("40", result[0]);
        }

        [TestMethod]
        public async Task Eio3HttpBinaryTest()
        {
            string uri = "http://localhost:11002/socket.io/?token=V3&EIO=3&transport=polling";
            byte[] arrOutput = { 0x00, 0x07, 0x05, 0xFF, 0x34, 0x35, 0x32, 0x2D, 0x5B, 0x22, 0x77, 0x65, 0x6C, 0x63, 0x6F, 0x6D, 0x65, 0x22, 0x2C, 0x7B, 0x22, 0x5F, 0x70, 0x6C, 0x61, 0x63, 0x65, 0x68, 0x6F, 0x6C, 0x64, 0x65, 0x72, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x2C, 0x22, 0x6E, 0x75, 0x6D, 0x22, 0x3A, 0x30, 0x7D, 0x2C, 0x7B, 0x22, 0x5F, 0x70, 0x6C, 0x61, 0x63, 0x65, 0x68, 0x6F, 0x6C, 0x64, 0x65, 0x72, 0x22, 0x3A, 0x74, 0x72, 0x75, 0x65, 0x2C, 0x22, 0x6E, 0x75, 0x6D, 0x22, 0x3A, 0x31, 0x7D, 0x5D, 0x01, 0x02, 0x09, 0xFF, 0x04, 0x77, 0x65, 0x6C, 0x63, 0x6F, 0x6D, 0x65, 0x20, 0x42, 0x71, 0x46, 0x63, 0x32, 0x53, 0x6F, 0x72, 0x67, 0x75, 0x6B, 0x4F, 0x32, 0x36, 0x2D, 0x6C, 0x41, 0x41, 0x41, 0x4A, 0x01, 0x05, 0xFF, 0x04, 0x74, 0x65, 0x73, 0x74 };
            var mockHttp = new MockHttpMessageHandler();
            mockHttp.When(uri)
                .Respond(req =>
                {
                    var content = new ByteArrayContent(arrOutput);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                    return new HttpResponseMessage
                    {
                        Content = content
                    };
                });
            var httpClient = mockHttp.ToHttpClient();

            var clientWebSocket = new Mock<IClientWebSocket>();

            var uriConverter = new Mock<IUriConverter>();
            uriConverter
                .Setup(x => x.GetHandshakeUri(It.IsAny<Uri>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(new Uri(uri));

            var texts = new List<string>();
            var bytes = new List<byte[]>();
            var transport = new HttpTransport(httpClient, 3)
            {
                OnTextReceived = text => texts.Add(text),
                OnBinaryReceived = b => bytes.Add(b)
            };
            await transport.GetAsync(uri, CancellationToken.None);

            await Task.Delay(100);

            Assert.AreEqual(1, texts.Count);
            Assert.AreEqual("452-[\"welcome\",{\"_placeholder\":true,\"num\":0},{\"_placeholder\":true,\"num\":1}]", texts[0]);
            Assert.AreEqual(2, bytes.Count);
            Assert.AreEqual("welcome BqFc2SorgukO26-lAAAJ", Encoding.UTF8.GetString(bytes[0]));
            Assert.AreEqual("test", Encoding.UTF8.GetString(bytes[1]));
        }
    }
}
