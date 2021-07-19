using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.JsonSerializer;
using System.Text;

namespace SocketIOClient.UnitTest
{
    [TestClass]
    public class SystemTextJsonSerializerTest
    {
        const string LONG_STRING = @"
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

        [TestMethod]
        public void TestEio3With1Byte()
        {
            var seriazlier = new SystemTextJsonSerializer(3);
            byte[] messageBytes = Encoding.UTF8.GetBytes(LONG_STRING + "xyz");
            var result = seriazlier.Serialize(new object[] {
                new
                {
                    Code = 404,
                    Message = messageBytes
                }
            });

            Assert.AreEqual("[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0}}]", result.Json);
            Assert.AreEqual(1, result.Bytes.Count);
            Assert.AreEqual(messageBytes.Length + 1, result.Bytes[0].Length);
            Assert.AreEqual(4, result.Bytes[0][0]);
            Assert.AreEqual(LONG_STRING + "xyz", Encoding.UTF8.GetString(result.Bytes[0], 1, result.Bytes[0].Length - 1));
        }

        [TestMethod]
        public void TestEio3With2Bytes()
        {
            var seriazlier = new SystemTextJsonSerializer(3);
            byte[] messageBytes = Encoding.UTF8.GetBytes(LONG_STRING + "xyz");
            byte[] dataBytes = Encoding.UTF8.GetBytes(LONG_STRING + "-data");
            var result = seriazlier.Serialize(new object[]
            {
                new
                {
                    Code = 404,
                    Message = messageBytes,
                    Data = dataBytes
                }
            });

            Assert.AreEqual("[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0},\"Data\":{\"_placeholder\":true,\"num\":1}}]", result.Json);
            Assert.AreEqual(2, result.Bytes.Count);
            Assert.AreEqual(messageBytes.Length + 1, result.Bytes[0].Length);
            Assert.AreEqual(4, result.Bytes[0][0]);
            Assert.AreEqual(LONG_STRING + "xyz", Encoding.UTF8.GetString(result.Bytes[0], 1, result.Bytes[0].Length - 1));
            Assert.AreEqual(dataBytes.Length + 1, result.Bytes[1].Length);
            Assert.AreEqual(4, result.Bytes[1][0]);
            Assert.AreEqual(LONG_STRING + "-data", Encoding.UTF8.GetString(result.Bytes[1], 1, result.Bytes[1].Length - 1));
        }

        [TestMethod]
        public void TestEio4With1Byte()
        {
            var seriazlier = new SystemTextJsonSerializer(4);
            byte[] messageBytes = Encoding.UTF8.GetBytes(LONG_STRING + "xyz");
            var result = seriazlier.Serialize(new object[]
            {
                new
                {
                    Code = 404,
                    Message = messageBytes
                }
            });

            Assert.AreEqual("[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0}}]", result.Json);
            Assert.AreEqual(messageBytes.Length, result.Bytes[0].Length);
            Assert.AreEqual(LONG_STRING + "xyz", Encoding.UTF8.GetString(result.Bytes[0]));
        }

        [TestMethod]
        public void TestEio4With2Bytes()
        {
            var seriazlier = new SystemTextJsonSerializer(4);
            byte[] messageBytes = Encoding.UTF8.GetBytes(LONG_STRING + "xyz");
            byte[] dataBytes = Encoding.UTF8.GetBytes(LONG_STRING + "-data");
            var result = seriazlier.Serialize(new object[]
            {
                new
                {
                    Code = 404,
                    Message = messageBytes,
                    Data = dataBytes
                }
            });

            Assert.AreEqual("[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0},\"Data\":{\"_placeholder\":true,\"num\":1}}]", result.Json);
            Assert.AreEqual(2, result.Bytes.Count);
            Assert.AreEqual(messageBytes.Length, result.Bytes[0].Length);
            Assert.AreEqual(LONG_STRING + "xyz", Encoding.UTF8.GetString(result.Bytes[0]));
            Assert.AreEqual(dataBytes.Length, result.Bytes[1].Length);
            Assert.AreEqual(LONG_STRING + "-data", Encoding.UTF8.GetString(result.Bytes[1]));
        }

        [TestMethod]
        public void TestEio4With2Bytes2Params()
        {
            var seriazlier = new SystemTextJsonSerializer(4);
            byte[] messageBytes = Encoding.UTF8.GetBytes(LONG_STRING + "xyz");
            byte[] dataBytes = Encoding.UTF8.GetBytes(LONG_STRING + "-data");
            var result = seriazlier.Serialize(new object[]
            {
                new
                {
                    Code = 404,
                    Message = messageBytes
                },
                dataBytes
            });

            Assert.AreEqual("[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0}},{\"_placeholder\":true,\"num\":1}]", result.Json);
            Assert.AreEqual(2, result.Bytes.Count);
            Assert.AreEqual(messageBytes.Length, result.Bytes[0].Length);
            Assert.AreEqual(LONG_STRING + "xyz", Encoding.UTF8.GetString(result.Bytes[0]));
            Assert.AreEqual(dataBytes.Length, result.Bytes[1].Length);
            Assert.AreEqual(LONG_STRING + "-data", Encoding.UTF8.GetString(result.Bytes[1]));
        }
    }
}
