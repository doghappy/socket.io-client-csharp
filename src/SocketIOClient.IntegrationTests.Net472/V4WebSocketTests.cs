using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using SocketIOClient.Transport;

namespace SocketIOClient.IntegrationTests.Net472
{
    [TestClass]
    public class V4WebSocketTests
    {
        [TestMethod]
        [DataRow("CustomHeader", "CustomHeader-Value")]
        [DataRow("User-Agent", "dotnet-socketio[client]/socket")]
        [DataRow("user-agent", "dotnet-socketio[client]/socket")]
        public async Task ExtraHeaders(string key, string value)
        {
            string actual = null;
            using (var io = new SocketIO(Common.Startup.V4_WS, new SocketIOOptions
            {
                Reconnection = false,
                EIO = EngineIO.V4,
                Transport = TransportProtocol.WebSocket,
                ExtraHeaders = new Dictionary<string, string>
                {
                    { key, value },
                },
            }))
            {
                await io.ConnectAsync();
                await io.EmitAsync("get_header",
                    res => actual = res.GetValue<string>(),
                    key.ToLower());
                await Task.Delay(100);

                actual.Should().Be(value);
            };
        }
    }
}
