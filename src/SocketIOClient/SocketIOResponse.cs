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
        private readonly IMessage2 _message2;

        public SocketIOResponse(IList<JsonElement> array, SocketIO socket)
        {
            _array = array;
            _socketIO = socket;
            PacketId = -1;
        }

        public SocketIOResponse(IMessage2 message2, SocketIO socket)
        {
            _message2 = message2;
            _socketIO = socket;
            PacketId = -1;
        }

        readonly IList<JsonElement> _array;

        public List<byte[]> InComingBytes => _message2.ReceivedBinary;

        private readonly SocketIO _socketIO;
        public int PacketId { get; set; }

        public T GetValue<T>(int index = 0)
        {
            return _socketIO.Serializer.Deserialize<T>(_message2, index);
        }

        public JsonElement GetValue(int index = 0) => _array[index];

        public int Count => _array.Count;

        public override string ToString()
        {
            return _socketIO.Serializer.MessageToJson(_message2);
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