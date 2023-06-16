using System;

namespace SocketIOClient.Extensions
{
    internal static class SocketIOEventExtensions
    {
        public static void TryInvoke<T>(this OnAnyHandler<T> handler, string eventName, SocketIOResponse<T> response)
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
        public static void TryInvoke<T>(this Action<SocketIOResponse<T>> handler, SocketIOResponse<T> response)
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
    }
}
