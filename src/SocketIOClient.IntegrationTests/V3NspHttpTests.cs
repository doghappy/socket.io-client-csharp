﻿using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SocketIOClient.IntegrationTests
{
    // [TestClass]
    public class V3NspHttpTests : HttpBaseTests
    {
        protected override string ServerUrl => V3_NSP_HTTP;
        protected override string ServerTokenUrl => V3_NSP_HTTP_TOKEN;
        protected override EngineIO EIO => EngineIO.V4;
    }
}