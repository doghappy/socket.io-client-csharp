using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public interface IHttpResponse
{
    string MediaType { get; }
    Task<byte[]> ReadAsByteArrayAsync();
    Task<string> ReadAsStringAsync();
}