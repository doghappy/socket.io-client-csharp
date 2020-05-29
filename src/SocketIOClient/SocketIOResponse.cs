using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SocketIOClient.JsonConverters;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient
{
    public class SocketIOResponse
    {
        public SocketIOResponse(JArray array, SocketIO socket)
        {
            _array = array;
            InComingBytes = new List<byte[]>();
            SocketIO = socket;
        }

        readonly JArray _array;

        public List<byte[]> InComingBytes { get; }
        public SocketIO SocketIO { get; }
        public int PacketId { get; set; }

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

        public async Task CallbackAsync(params object[] data)
        {
            await SocketIO.EmitCallbackAsync(PacketId, data);
        }
    }
}
