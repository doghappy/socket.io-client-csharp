using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class V4WebSocketTests : WebSocketBaseTests
    {
        protected override string ServerUrl => V4_WS;
        protected override string ServerTokenUrl => V4_WS_TOKEN;

        [TestMethod]
        public async Task Should_Be_Work_Even_An_Exception_Thrown_From_Handler()
        {
            using var io = CreateSocketIO();
            var results = new List<int>();
            io.On("1:emit", res => results.Add(6 / res.GetValue<int>()));

            await io.ConnectAsync();
            await io.EmitAsync("1:emit", 0);
            await io.EmitAsync("1:emit", 1);
            await Task.Delay(100);

            results.Should().Equal(6);
        }
    }
}