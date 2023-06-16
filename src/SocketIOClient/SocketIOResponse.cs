﻿using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient
{
    public class SocketIOResponse<T>
    {
        public SocketIOResponse(IList<T> array, SocketIO<T> socket)
        {
            _array = array;
            InComingBytes = new List<byte[]>();
            SocketIO = socket;
            PacketId = -1;
        }

        readonly IList<T> _array;

        public List<byte[]> InComingBytes { get; }
        public SocketIO<T> SocketIO { get; }
        public int PacketId { get; set; }

        //public T GetValue<T>(int index = 0)
        //{
        //    var element = GetValue(index);
        //    string json = element.GetRawText();
        //    return SocketIO.JsonSerializer.Deserialize<T>(json, InComingBytes);
        //}

        public T GetValue(int index = 0) => _array[index];

        public int Count => _array.Count;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append('[');
            foreach (var item in _array)
            {
                builder.Append(SocketIO.JsonSerializer.GetRawText(item));
                if (_array.IndexOf(item) < _array.Count - 1)
                {
                    builder.Append(',');
                }
            }
            builder.Append(']');
            return builder.ToString();
        }

        public async Task CallbackAsync(params object[] data)
        {
            await SocketIO.ClientAckAsync(PacketId, CancellationToken.None, data).ConfigureAwait(false);
        }

        public async Task CallbackAsync(CancellationToken cancellationToken, params object[] data)
        {
            await SocketIO.ClientAckAsync(PacketId, cancellationToken, data).ConfigureAwait(false);
        }
    }
}
