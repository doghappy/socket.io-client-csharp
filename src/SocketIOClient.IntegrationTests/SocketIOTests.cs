using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.IntegrationTests
{
    [TestClass]
    public class SocketIOTests
    {
        private SocketIO CreateSocketIO()
        {
            return new SocketIO("http://localhost:11040");
        }

        [TestMethod]
        public void Properties_Value_Should_Be_Correct_Before_Connected()
        {
            using var io = CreateSocketIO();

            io.Connected.Should().BeFalse();
            io.Id.Should().BeNull();
        }

        [TestMethod]
        public async Task Properties_Value_Should_Be_Changed_After_Connected()
        {
            using var io = CreateSocketIO();

            await io.ConnectAsync();

            io.Connected.Should().BeTrue();
            io.Id.Should().NotBeNull();
        }

        [TestMethod]
        [DataRow("1:emit", null, "[null]")]
        [DataRow("1:emit", true, "[true]")]
        [DataRow("1:emit", false, "[false]")]
        [DataRow("1:emit", -1234567890, "[-1234567890]")]
        [DataRow("1:emit", 1234567890, "[1234567890]")]
        [DataRow("1:emit", -1.234567890, "[-1.23456789]")]
        [DataRow("1:emit", 1.234567890, "[1.23456789]")]
        [DataRow("1:emit", "hello\n世界\n🌍🌎🌏", "[\"hello\\n世界\\n🌍🌎🌏\"]")]
        public async Task Should_Be_Able_To_Emit_1_Parameters_And_Emit_Back(string eventName, object data, string excepted)
        {
            using var io = CreateSocketIO();
            string? json = null;
            io.On(eventName, res => json = res.ToString());

            await io.ConnectAsync();
            await io.EmitAsync(eventName, data);
            await Task.Delay(100);

            json.Should().Be(excepted);
        }
    }
}