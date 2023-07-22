using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpMPTests : HttpMpTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override void ConfigureSerializerForEmitting1Parameter(SocketIO io)
        {
            throw new System.NotImplementedException();
        }

        protected override IEnumerable<(string EventName, object Data, string ExpectedJson, IEnumerable<byte[]>? ExpectedBytes)> Emit1ParameterTupleCases
        {
            get;
        }

        protected override string ServerUrl => Common.Startup.V2_HTTP_MP;
        protected override string ServerTokenUrl => Common.Startup.V2_HTTP_TOKEN_MP;
    }
}