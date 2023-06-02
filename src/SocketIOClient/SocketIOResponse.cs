using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SocketIO.Core;

namespace SocketIOClient
{
    public class SocketIOResponse
    {
        private readonly Message _msg;

        public SocketIOResponse(IList<JsonElement> array, SocketIO socket)
        {
            _array = array;
            _socketIO = socket;
            PacketId = -1;
        }
        
        public SocketIOResponse(Message msg, SocketIO socket)
        {
            _msg = msg;
            _socketIO = socket;
            PacketId = -1;
        }

        readonly IList<JsonElement> _array;

        public List<byte[]> InComingBytes => _msg.ReceivedBinary;

        private readonly SocketIO _socketIO;
        public int PacketId { get; set; }

        public T GetValue<T>(int index = 0)
        {
            var element = GetValue(index);
            string json = element.GetRawText();
            return _socketIO.Serializer.Deserialize<T>(json, InComingBytes);
        }

        public JsonElement GetValue(int index = 0) => _array[index];

        public int Count => _array.Count;

        public override string ToString()
        {
            return _socketIO.Serializer.MessageToJson(_msg);
        }

        public async Task CallbackAsync(params object[] data)
        {
            await _socketIO.ClientAckAsync(PacketId, CancellationToken.None, data).ConfigureAwait(false);
        }

        public async Task CallbackAsync(CancellationToken cancellationToken, params object[] data)
        {
            await _socketIO.ClientAckAsync(PacketId, cancellationToken, data).ConfigureAwait(false);
        }
    }
}
