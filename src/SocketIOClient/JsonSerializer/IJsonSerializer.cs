using System;
using System.Collections.Generic;

namespace SocketIOClient.JsonSerializer
{
    public interface IJsonSerializer
    {
        JsonSerializeResult Serialize(object[] data);
        T Deserialize<T>(string json);
        object Deserialize(string json, Type type);
        T Deserialize<T>(string json, IList<byte[]> incomingBytes);
        object Deserialize(string json, Type type, IList<byte[]> incomingBytes);

        List<T> GetListOfElementsFromRoot<T>(string json);

        string GetString<T>(T Json);

        T GetRootElement<T>(string json);
        int GetInt32FromJsonElement<T>(T element, string message, string propertyName);

        T GetProperty<T>(T element, string propertyName);

        List<T> GetListOfElements<T>(T element);

        string GetRawText<T>(T element);

    }
}