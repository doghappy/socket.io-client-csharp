using System;
using System.Collections.Concurrent;

namespace SocketIO.Serializer.MessagePack;

public static class TypeExtensions
{
    private static readonly ConcurrentDictionary<Type, bool> IsSimpleTypeCache = new();

    public static bool IsSimpleType(this Type type)
    {
        return IsSimpleTypeCache.GetOrAdd(type, t =>
            type.IsPrimitive ||
            type.IsEnum ||
            type == typeof(string) ||
            type == typeof(decimal) ||
            type == typeof(DateTime) ||
            // type == typeof(DateOnly) ||
            // type == typeof(TimeOnly) ||
            type == typeof(DateTimeOffset) ||
            type == typeof(TimeSpan) ||
            type == typeof(Guid) ||
            IsNullableSimpleType(type));

        static bool IsNullableSimpleType(Type t)
        {
            var underlyingType = Nullable.GetUnderlyingType(t);
            return underlyingType != null && IsSimpleType(underlyingType);
        }
    }
}