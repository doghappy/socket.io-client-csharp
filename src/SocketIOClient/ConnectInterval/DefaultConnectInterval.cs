namespace SocketIOClient.ConnectInterval
{
    class DefaultConnectInterval: IConnectInterval
    {
        public DefaultConnectInterval(SocketIOOptions options)
        {
            this.options = options;
            delayDouble = options.ReconnectionDelay;
        }

        readonly SocketIOOptions options;
        double delayDouble;

        public int GetDelay()
        {
            return (int)delayDouble;
        }

        public double NextDealy()
        {
            delayDouble += 2 * options.RandomizationFactor;
            return delayDouble;
        }
    }
}
