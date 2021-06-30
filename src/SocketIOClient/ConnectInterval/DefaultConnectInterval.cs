using System;

namespace SocketIOClient.ConnectInterval
{
    class DefaultConnectInterval: IConnectInterval
    {
        public DefaultConnectInterval(SocketIOOptions options)
        {
            this.options = options;
            this.delay = options.ReconnectionDelay;
        }

        readonly SocketIOOptions options;
        private double delay;
        private int attempts = 0;

        public double GetDelay()
        {
            return this.delay;
        }

        public double NextDelay()
        {
            this.delay = options.ReconnectionDelay * (long)Math.Pow(2, attempts++);
            
            if (this.options.RandomizationFactor > 0) 
            {
                Random random = new Random();
                var deviation = (long)Math.Floor(random.NextDouble() * this.options.RandomizationFactor * options.ReconnectionDelay);
                this.delay = ((long)Math.Floor(random.NextDouble() * 10) & 1) == 0 ? this.delay - deviation : this.delay + deviation;
            }

            return this.delay;
        }
    }
}
