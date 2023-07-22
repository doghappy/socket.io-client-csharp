using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SocketIO.Core;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V2HttpTests : HttpTests
    {
        protected override EngineIO EIO => EngineIO.V3;
        protected override SocketIO CreateSocketIO(SocketIOOptions options)
        {
            throw new System.NotImplementedException();
        }

        protected override SocketIO CreateTokenSocketIO(SocketIOOptions options)
        {
            throw new System.NotImplementedException();
        }

        protected override void ConfigureSerializerForEmitting1Parameter(SocketIO io)
        {
            throw new System.NotImplementedException();
        }

        protected override IEnumerable<(string EventName, object Data, string ExpectedJson, IEnumerable<byte[]>? ExpectedBytes)> Emit1ParameterTupleCases
        {
            get;
        }

        protected override string ServerUrl => Common.Startup.V2_HTTP;
        protected override string ServerTokenUrl => Common.Startup.V2_HTTP_TOKEN;
    }
}