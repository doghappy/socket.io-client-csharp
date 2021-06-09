using SocketIOClient.Response;
using System;
using System.Text.Json;

namespace SocketIOClient.SocketIOSerializer
{
    public class OpenSerializer : ISocketIOSeriazlier<OpenResponse>
    {
        public OpenResponse Read(string text)
        {
            if (text.StartsWith("{\"sid\":\""))
            {
                var doc = JsonDocument.Parse(text);
                var root = doc.RootElement;
                return new OpenResponse
                {
                    Sid = root.GetProperty("sid").GetString(),
                    PingInterval = root.GetProperty("pingInterval").GetInt32(),
                    PingTimeout = root.GetProperty("pingTimeout").GetInt32()
                };
            }
            return null;
        }

        public void Write()
        {
            throw new NotImplementedException();
        }
    }
}
