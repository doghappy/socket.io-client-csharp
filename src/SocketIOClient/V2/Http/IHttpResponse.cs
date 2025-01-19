using System.Threading.Tasks;

namespace SocketIOClient.V2.Http;

public interface IHttpResponse
{
    string MediaType { get; }
    Task<byte[]> ReadAsByteArrayAsync();
    Task<string> ReadAsStringAsync();
}