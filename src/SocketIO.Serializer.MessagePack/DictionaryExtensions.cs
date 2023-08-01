using System;
using System.Collections.Generic;
using System.Reflection;

namespace SocketIO.Serializer.MessagePack;

static class DictionaryExtensions
{
    public static T ToObject<T>(this IDictionary<object, object> dictionary)
    {
        var type = typeof(T);
        var obj = dictionary.ToObject(type);
        return (T)obj;
    }

    public static object ToObject(this IDictionary<object, object> dictionary, Type type)
    {
        var obj = Activator.CreateInstance(type);

        var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        foreach (var prop in props)
        {
            if (!dictionary.TryGetValue(prop.Name, out var value))
                continue;
            if (prop.PropertyType == typeof(byte[]) && value is string base64)
            {
                var bytes = Convert.FromBase64String(base64);
                prop.SetValue(obj, bytes);
                continue;
            }

            if (!prop.PropertyType.IsArray && !prop.PropertyType.IsSimpleType())
            {
                var dic = ((IDictionary<object, object>)value).ToObject(prop.PropertyType);
                prop.SetValue(obj, dic);
                continue;
            }

            prop.SetValue(obj, value);
        }

        return obj;
    }
}