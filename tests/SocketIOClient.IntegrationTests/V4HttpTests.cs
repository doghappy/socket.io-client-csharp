using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIOClient.IntegrationTests.Utils;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4HttpTests : SocketIOTests
    {
        protected override EngineIO EIO => EngineIO.V4;
        protected override string ServerUrl => Common.Startup.V4_HTTP;
        protected override string ServerTokenUrl => Common.Startup.V4_HTTP_TOKEN;

        protected override TransportProtocol Transport => TransportProtocol.Polling;
        
        protected override SocketIO CreateSocketIO()
        {
            var options = new SocketIOOptions
            {
                EIO = EIO,
                AutoUpgrade = false,
                Reconnection = false,
                ConnectionTimeout = TimeSpan.FromSeconds(2)
            };
            var io = new SocketIO(ServerUrl, options);
            io.SetMessagePackSerializer();
            return io;
        }

        protected override SocketIO CreateSocketIO(SocketIOOptions options)
        {
            throw new NotImplementedException();
        }

        protected override SocketIO CreateTokenSocketIO(SocketIOOptions options)
        {
            throw new NotImplementedException();
        }

        protected override void ConfigureSerializerForEmitting1Parameter(SocketIO io)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<(string EventName, object Data, string ExpectedJson, IEnumerable<byte[]>? ExpectedBytes)> Emit1ParameterTupleCases
        {
            get;
        }
    }
}