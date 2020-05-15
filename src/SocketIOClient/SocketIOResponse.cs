using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient.JsonConverters;
using System.Collections.Generic;

namespace SocketIOClient
{
    public class SocketIOResponse
    {
        public SocketIOResponse(JArray array)
        {
            _array = array;
            InComingBytes = new List<byte[]>();
        }

        readonly JArray _array;

        public List<byte[]> InComingBytes { get; }

        public T GetValue<T>(int index = 0)
        {
            var token = GetValue(index);
            if (token.Type == JTokenType.Object)
            {
                string json = token.ToString();
                return JsonConvert.DeserializeObject<T>(json, new ByteArrayConverter
                {
                    InComingBytes = InComingBytes
                });
            }
            else
            {
                return token.ToObject<T>();
            }
        }

        public JToken GetValue(int index = 0)
        {
            return _array[index];
        }

        public int Count => _array.Count;

        public override string ToString()
        {
            return _array.ToString();
        }
    }
}
