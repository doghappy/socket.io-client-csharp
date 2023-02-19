using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIOClient.JsonSerializer;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;

namespace SocketIOClient.UnitTests;

[TestClass]
public class SystemTextJsonSerializerTest
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
        var serializer = new SystemTextJsonSerializer();
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
    public void Should_use_options_to_serialize(object[] data, JsonSerializerOptions options, string expectedJson)
    {
        var serializer = new SystemTextJsonSerializer(options);
        serializer.Serialize(data).Json
            .Should().Be(expectedJson);
    }

    private static IEnumerable<object[]> OptionsCases =>
        OptionsTupleCases.Select(x => new object[] { x.data, x.options, x.expectedJson });

    private static IEnumerable<(object[] data, JsonSerializerOptions options, string expectedJson)> OptionsTupleCases
    {
        get
        {
            return new (object[] data, JsonSerializerOptions options, string expectedJson)[]
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
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        Converters =
                        {
                            new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
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
        var serializer = new SystemTextJsonSerializer();
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
        var serializer = new SystemTextJsonSerializer();
        serializer.Deserialize<Person>(json, bytes)
            .Should().BeEquivalentTo(new Person
            {
                Id = 401,
                Name = "Qiangdong Liu 🐮",
                Attach = new byte[] { 4, 3, 2, 1, },
            });
    }
}