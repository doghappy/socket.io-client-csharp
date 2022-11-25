﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpTests : HttpBaseTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override string ServerUrl => V2_HTTP;
        protected override string ServerTokenUrl => V2_HTTP_TOKEN;
    }
}