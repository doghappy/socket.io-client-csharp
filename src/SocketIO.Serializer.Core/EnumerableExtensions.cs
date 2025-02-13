using System.Collections.Generic;
using System.Linq;

namespace SocketIO.Serializer.Core;

public static class EnumerableExtensions
{
    public static bool AnyBinary(this IEnumerable<SerializedItem> serializedItems)
    {
        return serializedItems.Any(x => x.Type == SerializedMessageType.Binary);
    }

    public static string FirstText(this IEnumerable<SerializedItem> serializedItems)
    {
        return serializedItems.First(x => x.Type == SerializedMessageType.Text).Text;
    }

    public static List<byte[]> AllBinary(this IEnumerable<SerializedItem> serializedItems)
    {
        return serializedItems
            .Where(x => x.Type == SerializedMessageType.Binary)
            .Select(x => x.Binary)
            .ToList();
    }
}