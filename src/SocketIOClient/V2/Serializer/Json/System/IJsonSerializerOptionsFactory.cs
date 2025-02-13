using System.Text.Json;

namespace SocketIOClient.V2.Serializer.Json.System;

public interface IJsonSerializerOptionsFactory
{
    JsonSerializerOptions New(JsonSerializerOptions options);
}