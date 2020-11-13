using SocketIOClient.WebSocketClient;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SocketIOClient.EioHandler
{
    class Eio3Handler : IEioHandler
    {
        CancellationTokenSource _pingToken;
        internal DateTime PingTime;
        internal int PingInterval;

        public async Task IOConnectAsync(SocketIO io)
        {
            var builder = new StringBuilder();
            builder.Append("40");

            if (!string.IsNullOrEmpty(io.Namespace))
            {
                builder.Append(io.Namespace.TrimEnd(','));
            }
            if (io.Options.Query != null && io.Options.Query.Count > 0)
            {
                builder.Append('?');
                int index = -1;
                foreach (var item in io.Options.Query)
                {
                    index++;
                    builder
                        .Append(item.Key)
                        .Append('=')
                        .Append(item.Value);
                    if (index < io.Options.Query.Count - 1)
                    {
                        builder.Append('&');
                    }
                }
            }
            if (!string.IsNullOrEmpty(io.Namespace))
            {
                builder.Append(',');
            }
            await io.Socket.SendMessageAsync(builder.ToString());
        }

        public void Unpack(SocketIO io, string text)
        {
            if (string.IsNullOrEmpty(io.Namespace))
            {
                if (text == string.Empty)
                {
                    io.InvokeConnect();
                }
            }
            else
            {
                if (text == io.Namespace)
                {
                    io.InvokeConnect();
                }
            }
        }

        public void StartPingInterval(SocketIO io)
        {
            _pingToken = new CancellationTokenSource();
            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    await Task.Delay(PingInterval);
                    if (_pingToken.IsCancellationRequested)
                        return;
                    try
                    {
                        PingTime = DateTime.Now;
                        await io.Socket.SendMessageAsync("2");
                        io.InvokePingV3();
                    }
                    catch (Exception ex) { Trace.TraceError(ex.ToString()); }
                }
            }, _pingToken.Token);
        }

        public void StopPingInterval()
        {
            if (_pingToken != null)
            {
                _pingToken.Cancel();
            }
        }
    }
}
