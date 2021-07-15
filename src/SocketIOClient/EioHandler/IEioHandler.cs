using System.Collections.Generic;

namespace SocketIOClient.EioHandler
{
    public interface IEioHandler
    {
        string CreateConnectionMessage(string @namespace, Dictionary<string, string> query);
        ConnectionResult CheckConnection(string @namespace, string text);
        string GetErrorMessage(string text);
        byte[] GetBytes(byte[] bytes);
    }
}
