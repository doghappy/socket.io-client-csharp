using SocketIOClient.JsonSerializer;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace SocketIOClient
{
    class SioResponse
    {
        public string Event { get; set; }

        public int PacketId { get; set; }

        public List<JsonElement> JsonElements { get; set; }

        public IJsonSerializer JsonSerializer { get; set; }

        public List<byte[]> InComingBytes { get; }

        public T GetValue<T>(int index = 0)
        {
            var element = GetValue(index);
            string json = element.GetRawText();
            return JsonSerializer.Deserialize<T>(json, InComingBytes);
        }

        public JsonElement GetValue(int index = 0) => JsonElements[index];

        public override string ToString()
        {
            if (JsonElements == null)
            {
                return "null";
            }

            var builder = new StringBuilder();
            builder.Append('[');
            foreach (var item in JsonElements)
            {
                builder.Append(item.GetRawText());
                if (JsonElements.IndexOf(item) < JsonElements.Count - 1)
                {
                    builder.Append(',');
                }
            }
            builder.Append(']');
            return builder.ToString();
        }
    }
}
