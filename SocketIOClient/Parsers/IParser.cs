using System.Threading.Tasks;

namespace SocketIOClient.Parsers
{
    public interface IParser
    {
        Task ParseAsync(ResponseTextParser rtp);
    }
}
