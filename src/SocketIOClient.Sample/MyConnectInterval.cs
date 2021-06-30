using SocketIOClient.ConnectInterval;
using System;

namespace SocketIOClient.Sample
{
    class MyConnectInterval : IConnectInterval
    {
        public MyConnectInterval()
        {
            delay = 1000;
        }

        double delay;

        public double GetDelay()
        {
            Console.WriteLine("GetDelay: " + delay);
            return delay;
        }

        public double NextDelay()
        {
            Console.WriteLine("NextDelay: " + (delay + 1000));
            return delay += 1000;
        }
    }
}
