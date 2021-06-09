using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace SocketIOClient.Test.SocketIOTests.V3
{
    //[TestClass]
    //public class OnErrorV3NspTest : OnErrorTest
    //{
    //    public OnErrorV3NspTest()
    //    {
    //        SocketIOCreator = new ScoketIOV3NspCreator();
    //    }

    //    protected override ISocketIOCreateable SocketIOCreator { get; }

    //    [TestMethod]
    //    public override async Task Test()
    //    {
    //        bool connected = false;
    //        string error = null;
    //        var client = new SocketIO(SocketIOCreator.Url, new SocketIOOptions
    //        {
    //            Reconnection = false,
    //            EIO = SocketIOCreator.EIO
    //        });
    //        client.OnConnected += (sender, e) => connected = true;
    //        client.OnError += (sender, e) => error = e;
    //        await client.ConnectAsync();
    //        await Task.Delay(200);

    //        await client.DisconnectAsync();

    //        Assert.AreEqual("{\"message\":\"Authentication error\"}", error);
    //    }
    //}
}
