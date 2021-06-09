using System;
using System.Text.Json;

namespace SocketIOClient.SocketIOSerializer
{
    //public class DisconnectionSerializer : ISocketIOSeriazlier
    //{
    //    public void Read(SocketIO socket, string text)
    //    {
    //        if (string.IsNullOrEmpty(socket.Namespace))
    //        {
    //            if (text == string.Empty)
    //            {
    //                socket.InvokeDisconnect("io server disconnect");
    //            }
    //        }
    //        else
    //        {
    //            if (text == socket.Namespace)
    //            {
    //                socket.InvokeDisconnect("io server disconnect");
    //            }
    //        }
    //    }

    //    public void Write()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public OnOpenedHandler OnOpenedHandler { get; set; }
    //}
}
