using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIO.Serializer.SystemTextJson.Tests;

public class SystemTextJsonSerializerTests
{
    private static IEnumerable<(
            string eventName,
            string ns,
            EngineIO eio,
            object[] data,
            IEnumerable<SerializedItem> expectedItems)>
        SerializeTextTupleCases =>
        new (string eventName, string ns, EngineIO eio, object[] data, IEnumerable<SerializedItem> expectedItems)[]
        {
            (
                "test",
                string.Empty,
                EngineIO.V3,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\"]"
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V3,
                new object[1],
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42[\"test\",null]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                Array.Empty<object>(),
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp[\"test\"]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[] { true },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp[\"test\",true]"
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[] { true, false, 123 },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "42nsp[\"test\",true,false,123]"
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V3,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"test\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    }
                }),
            (
                "test",
                "nsp",
                EngineIO.V3,
                new object[]
                {
                    new byte[] { 1, 2, 3 },
                    new byte[] { 4, 5, 6 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "452-nsp[\"test\",{\"_placeholder\":true,\"num\":0},{\"_placeholder\":true,\"num\":1}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 4, 5, 6 }
                    }
                }),
            (
                "test",
                string.Empty,
                EngineIO.V4,
                new object[]
                {
                    new byte[] { 1, 2, 3 }
                },
                new SerializedItem[]
                {
                    new()
                    {
                        Type = SerializedMessageType.Text,
                        Text = "451-[\"test\",{\"_placeholder\":true,\"num\":0}]"
                    },
                    new()
                    {
                        Type = SerializedMessageType.Binary,
                        Binary = new byte[] { 1, 2, 3 }
                    }
                }),
        };

    public static IEnumerable<object[]> SerializeTextCases => SerializeTextTupleCases
        .Select((x, caseId) => new object[]
        {
            caseId,
            x.eventName,
            x.ns,
            x.eio,
            x.data,
            x.expectedItems
        });

    [Theory]
    [MemberData(nameof(SerializeTextCases))]
    public void Should_serialize_given_event_message_without_bytes(
        int caseId,
        string eventName,
        string ns,
        EngineIO eio,
        object[] data,
        IEnumerable<SerializedItem> expectedItems)
    {
        var serializer = new SystemTextJsonSerializer();
        var items = serializer.Serialize(eventName, ns, data);
        items.Should().BeEquivalentTo(expectedItems, config => config.WithStrictOrdering());
    }
}