using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;

namespace SocketIOClient.Test
{
    [TestClass]
    public class DataFormatterTest
    {
        [TestMethod]
        public void SingleStringTest()
        {
            string text = "\"string\"";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("\"string\"", list[0]);
        }

        [TestMethod]
        public void SingleTrueTest()
        {
            string text = "true";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("true", list[0]);
        }

        [TestMethod]
        public void SingleFalseTest()
        {
            string text = "false";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("false", list[0]);
        }

        [TestMethod]
        public void SingleNullTest()
        {
            string text = "null";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual("null", list[0]);
        }

        [TestMethod]
        public void SingleNumberTest()
        {
            string text = new Random().Next().ToString();

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(text, list[0]);
        }

        [TestMethod]
        public void StringNumberTest()
        {
            int number = new Random().Next();
            string text = $"\"test\",{number}";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual("\"test\"", list[0]);
            Assert.AreEqual(number.ToString(), list[1]);
        }

        [TestMethod]
        public void StringFalseNumberTureTest()
        {
            int number = new Random().Next();
            string text = $"\"test\",false,{number},true";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(4, list.Count);
            Assert.AreEqual("\"test\"", list[0]);
            Assert.AreEqual("false", list[1]);
            Assert.AreEqual(number.ToString(), list[2]);
            Assert.AreEqual("true", list[3]);
        }

        [TestMethod]
        public void ObjectTest()
        {
            var obj = new
            {
                code = 200,
                data = "{ httpStatusCode: 401, message: \"throw new Exception(\"test\")\" }"
            };
            string text = JsonConvert.SerializeObject(obj);

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(1, list.Count);
            Assert.AreEqual(text, list[0]);
        }

        [TestMethod]
        public void ObjectStringTest()
        {
            var obj = new
            {
                code = 200,
                data = "{ httpStatusCode: 401, message: \"throw new Exception(\"test\")\" }"
            };
            var json = JsonConvert.SerializeObject(obj);
            string text = json + ",\"abc\"";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(json, list[0]);
            Assert.AreEqual("\"abc\"", list[1]);
        }

        [TestMethod]
        public void ObjectNullObjectStringNumberObjectFalseTest()
        {
            var obj1 = new
            {
                Windows = new[]
                {
                    "XP", "Windows 7", "Windows 10"
                },
                Languages = new[]
                {
                    new
                    {
                        Name = "Chinese",
                        Test = "\"中文\""
                    },
                    new
                    {
                        Name = "English",
                        Test = "\"英文\""
                    }
                }
            };
            var obj2 = new { };
            var obj3 = new
            {
                code = 200,
                data = "{ httpStatusCode: 401, message: \"throw new Exception(\"test\")\" }",
                obj = new
                {
                    httpStatusCode = 401,
                    message = "throw new Exception(\"test\")"
                }
            };
            string json1 = JsonConvert.SerializeObject(obj1);
            string json2 = JsonConvert.SerializeObject(obj2);
            string json3 = JsonConvert.SerializeObject(obj3);
            string text = $"{json1},null,{json2},\"qwer\",110,{json3},false";

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(7, list.Count);
            Assert.AreEqual(json1, list[0]);
            Assert.AreEqual("null", list[1]);
            Assert.AreEqual(json2, list[2]);
            Assert.AreEqual("\"qwer\"", list[3]);
            Assert.AreEqual("110", list[4]);
            Assert.AreEqual(json3, list[5]);
            Assert.AreEqual("false", list[6]);
        }

        [TestMethod]
        public void ArrayTest()
        {
            var array = new[]
            {
                new[]
                {
                    new
                    {
                        code = 200,
                        data = "{ httpStatusCode: 401, message: \"throw new Exception(\"test\")\" }",
                        test = new[] { "a", "\"", "\"\"[]", "{{}}[][][{}[}[][[][][{}[[][][{}[" }
                    },
                    new
                    {
                        code = 200,
                        data = "{ httpStatusCode: 401, message: \"throw new Exception(\"test\")\" }",
                        test = new[] { "a", "\"", "\"\"[]", "{{}}[][]}}[][}}[][]}}[][[[][}}[][]}}[][{}[" }
                    }
                }
            };
            string json = JsonConvert.SerializeObject(array);
            string text = json + ",\"test\",123,true," + json;

            var list = new DataFormatter().Format(text);

            Assert.AreEqual(5, list.Count);
            Assert.AreEqual(json, list[0]);
            Assert.AreEqual("\"test\"", list[1]);
            Assert.AreEqual("123", list[2]);
            Assert.AreEqual("true", list[3]);
            Assert.AreEqual(json, list[4]);
        }
    }
}
