using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using SocketIOClient.JsonSerializer;

namespace SocketIOClient.Newtonsoft.Json.UnitTests;

[TestClass]
public class NewtonsoftJsonSerializerTest
{
const string LongString = @"
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
    [DynamicData(nameof(SerializeCases))]
    public void Should_serialize_object_even_contains_bytes(object[] data, JsonSerializeResult expected)
    {
        var serializer = new NewtonsoftJsonSerializer();
        serializer.Serialize(data)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<object[]> SerializeCases =>
        SerializeTupleCases.Select(x => new object[] { x.data, x.expected });

    private static IEnumerable<(object[] data, JsonSerializeResult expected)> SerializeTupleCases
    {
        get
        {
            return new (object[] data, JsonSerializeResult expected)[]
            {
                (
                    new object[]
                    {
                        new
                        {
                            Code = 404,
                            Message = Encoding.UTF8.GetBytes(LongString + "xyz")
                        }
                    },
                    new JsonSerializeResult
                    {
                        Json = "[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0}}]",
                        Bytes = new List<byte[]>
                        {
                            Encoding.UTF8.GetBytes(LongString + "xyz")
                        },
                    }),
                (
                    new object[]
                    {
                        new
                        {
                            Code = 404,
                            Message = Encoding.UTF8.GetBytes(LongString + "🌏🌍🌎"),
                            Data = Encoding.UTF8.GetBytes(LongString + "😁👍"),
                        }
                    },
                    new JsonSerializeResult
                    {
                        Json =
                            "[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0},\"Data\":{\"_placeholder\":true,\"num\":1}}]",
                        Bytes = new List<byte[]>
                        {
                            Encoding.UTF8.GetBytes(LongString + "🌏🌍🌎"),
                            Encoding.UTF8.GetBytes(LongString + "😁👍"),
                        },
                    }),
                (
                    new object[]
                    {
                        new
                        {
                            Code = 404,
                            Message = Encoding.UTF8.GetBytes(LongString + "🌏🌍🌎"),
                        },
                        Encoding.UTF8.GetBytes(LongString + "😁👍"),
                    },
                    new JsonSerializeResult
                    {
                        Json =
                            "[{\"Code\":404,\"Message\":{\"_placeholder\":true,\"num\":0}},{\"_placeholder\":true,\"num\":1}]",
                        Bytes = new List<byte[]>
                        {
                            Encoding.UTF8.GetBytes(LongString + "🌏🌍🌎"),
                            Encoding.UTF8.GetBytes(LongString + "😁👍"),
                        },
                    }),
            };
        }
    }

    [TestMethod]
    [DynamicData(nameof(OptionsCases))]
    public void Should_use_options_to_serialize(object[] data, JsonSerializerSettings settings, string expectedJson)
    {
        var serializer = new NewtonsoftJsonSerializer(settings);
        serializer.Serialize(data).Json
            .Should().Be(expectedJson);
    }

    private static IEnumerable<object[]> OptionsCases =>
        OptionsTupleCases.Select(x => new object[] { x.data, x.settings, x.expectedJson });

    private static IEnumerable<(object[] data, JsonSerializerSettings settings, string expectedJson)> OptionsTupleCases
    {
        get
        {
            return new (object[] data, JsonSerializerSettings settings, string expectedJson)[]
            {
                (
                    new object[]
                    {
                        new
                        {
                            Code = HttpStatusCode.InternalServerError,
                            Message = "hello world!",
                        }
                    },
                    new JsonSerializerSettings
                    {
                        ContractResolver = new DefaultContractResolver()
                        {
                            NamingStrategy = new CamelCaseNamingStrategy(),
                        },
                        Converters =
                        {
                            new StringEnumConverter(new CamelCaseNamingStrategy()),
                        }
                    },
                    "[{\"code\":\"internalServerError\",\"message\":\"hello world!\"}]"),
            };
        }
    }

    class Person
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public byte[] Attach { get; set; }
    }

    [TestMethod]
    [DynamicData(nameof(DeserializeCases))]
    public void Should_deserialize_object_even_contains_bytes(string json,
        IList<byte[]> bytes,
        Type type,
        object expected)
    {
        var serializer = new NewtonsoftJsonSerializer();
        serializer.Deserialize(json, type, bytes)
            .Should().BeEquivalentTo(expected);
    }

    private static IEnumerable<object[]> DeserializeCases =>
        DeserializeTupleCases.Select(x => new object[] { x.json, x.bytes, x.type, x.expected });

    private static IEnumerable<(string json, IList<byte[]> bytes, Type type, object expected)> DeserializeTupleCases
    {
        get
        {
            return new (string json, IList<byte[]> bytes, Type type, object expected)[]
            {
                (
                    "{\"Id\":404,\"Name\":\"Jack Ma 😎\",\"Attach\":{\"_placeholder\":true,\"num\":0}}",
                    new List<byte[]>
                    {
                        new byte[] { 1, 2, 3, 4, }
                    },
                    typeof(Person),
                    new Person
                    {
                        Id = 404,
                        Name = "Jack Ma 😎",
                        Attach = new byte[] { 1, 2, 3, 4, },
                    }),
            };
        }
    }

    [TestMethod]
    public void Should_deserialize_generic_type_even_contains_bytes()
    {
        const string json = "{\"Id\":401,\"Name\":\"Qiangdong Liu 🐮\",\"Attach\":{\"_placeholder\":true,\"num\":0}}";
        var bytes = new List<byte[]>
        {
            new byte[] { 4, 3, 2, 1, }
        };
        var serializer = new NewtonsoftJsonSerializer();
        serializer.Deserialize<Person>(json, bytes)
            .Should().BeEquivalentTo(new Person
            {
                Id = 401,
                Name = "Qiangdong Liu 🐮",
                Attach = new byte[] { 4, 3, 2, 1, },
            });
    }
}