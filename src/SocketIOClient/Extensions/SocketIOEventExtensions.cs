using System;
using System.Threading.Tasks;

namespace SocketIOClient.Extensions
{
    internal static class SocketIOEventExtensions
    {
        public static void TryInvoke(this OnAnyHandler handler, string eventName, SocketIOResponse response)
        {
            Task.Run(() => handler(eventName, response));
        }

        public static void TryInvoke(this Action<SocketIOResponse> handler, SocketIOResponse response)
        {
            Task.Run(() => handler(response));
        }

        public static void TryInvoke(this Func<SocketIOResponse, Task> handler, SocketIOResponse response)
        {
            handler(response);
        }
    }
}