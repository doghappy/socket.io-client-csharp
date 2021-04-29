using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace SocketIOClient
{
    public class SocketIOResponse
    {
        public SocketIOResponse(IList<JsonElement> array, SocketIO socket)
        {
            _array = array;
            InComingBytes = new List<byte[]>();
            SocketIO = socket;
        }

        readonly IList<JsonElement> _array;

        public List<byte[]> InComingBytes { get; }
        public SocketIO SocketIO { get; }
        public int PacketId { get; set; }

        public T GetValue<T>(int index = 0)
        {
            var element = GetValue(index);
            string json = element.GetRawText();
            return SocketIO.JsonSerializer.Deserialize<T>(json, InComingBytes);
        }

        public JsonElement GetValue(int index = 0) => _array[index];

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
