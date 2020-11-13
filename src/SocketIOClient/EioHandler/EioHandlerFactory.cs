using System;

namespace SocketIOClient.EioHandler
{
    static class EioHandlerFactory
    {
        internal static IEioHandler GetHandler(int eio)
        {
            switch (eio)
            {
                case 3:
                    return new Eio3Handler();
                case 4:
                    return new Eio4Handler();
                default:
                    throw new ArgumentException("Invalid EIO");
            }
        }
    }
}
