using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SocketIOClient.Processors;
using SocketIOClient.WebSocketClient;
using System.Text;

namespace SocketIOClient.UnitTest.SocketIOTests
{
    [TestClass]
    public class OnBinaryReceivedTest
    {
        //[TestMethod]
        //public void Eio4Binary()
        //{
        //    var mockProcessor = new Mock<Processor>();
        //    var mockWebSocketClient = new Mock<IWebSocketClient>();
        //    //mockWebSocketClient.SetupAllProperties();
        //    mockWebSocketClient.SetupProperty(x => x.OnBinaryReceived);
        //    var io = new SocketIO
        //    {
        //        MessageProcessor = mockProcessor.Object,
        //        Socket = mockWebSocketClient.Object
        //    };
        //    mockWebSocketClient.Object.OnBinaryReceived(Encoding.UTF8.GetBytes("hello world!"));
        //    var snapshot = io.GetLowLevelEventsSnapshot();

        //    mockProcessor.Verify(p => p.Process(It.IsAny<MessageContext>()), Times.Once());
        //    mockWebSocketClient.Verify(p => p.OnTextReceived, Times.Never());
        //    mockWebSocketClient.Verify(p => p.OnBinaryReceived, Times.Once());

        //    //Assert.AreEqual(1, snapshot.Count);
        //    //Assert.AreEqual("hello world!", Encoding.UTF8.GetString(snapshot[0].Response.));
        //}
    }
}
