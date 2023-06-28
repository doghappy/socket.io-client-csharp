using System;
using System.Threading.Tasks;

namespace SocketIOClient.Extensions
{
    internal static class SocketIOEventExtensions
    {
        public static void TryInvoke(this OnAnyHandler handler, string eventName, SocketIOResponse response)
        {
            try
            {
                handler(eventName, response);
            }
            catch
            {
                // The exception is thrown by the user code, so it can be swallowed
            }
        }

        public static void TryInvoke(this Action<SocketIOResponse> handler, SocketIOResponse response)
        {
            try
            {
                handler(response);
            }
            catch
            {
                // The exception is thrown by the user code, so it can be swallowed
            }
        }

        public static async Task TryInvoke(this Func<SocketIOResponse, Task> handler, SocketIOResponse response)
        {
            try
            {
                await handler(response);
            }
            catch
            {
                // The exception is thrown by the user code, so it can be swallowed
            }
        }
    }
}