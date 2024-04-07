﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;
using SocketIO.Client.Transport;

namespace SocketIO.Client.IntegrationTests
{
    [TestClass]
    public class V2WebSocketTests : SystemTextJsonTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override TransportProtocol Transport => TransportProtocol.WebSocket;
        protected override string ServerUrl => Common.Startup.V2_WS;
        protected override string ServerTokenUrl => Common.Startup.V2_WS_TOKEN;
    }
}