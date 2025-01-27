using System.Net.Http;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public class SystemHttpResponse(HttpResponseMessage response) : IHttpResponse
{
    public string MediaType => response.Content.Headers.ContentType?.MediaType;
    
    public Task<byte[]> ReadAsByteArrayAsync()
    {
        return response.Content.ReadAsByteArrayAsync();
    }

    public Task<string> ReadAsStringAsync()
    {
        return response.Content.ReadAsStringAsync();
    }
}