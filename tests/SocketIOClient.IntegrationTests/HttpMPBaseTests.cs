using System;
using MessagePack.Resolvers;
using SocketIO.Serializer.MessagePack;

namespace SocketIOClient.IntegrationTests
{
    public abstract class HttpMPBaseTests : HttpBaseTests
    {
        protected override SocketIO CreateSocketIO(SocketIOOptions options)
        {
            var io = new SocketIO(ServerUrl, options);
            SetSerializer(io);
            return io;
        }

        protected override SocketIO CreateTokenSocketIO(SocketIOOptions options)
        {
            var io = new SocketIO(ServerTokenUrl, options);
            SetSerializer(io);
            return io;
        }

        private void SetSerializer(SocketIO io)
        {
            io.Serializer = new SocketIOMessagePackSerializer(ContractlessStandardResolver.Options, io.Options.EIO);
        }
    }
}