using System.Net.Http;
using System.Threading.Tasks;

namespace SocketIOClient.V2.Protocol.Http;

public class SystemHttpResponse(HttpResponseMessage response) : IHttpResponse
{
    public string MediaType => response.Content.Headers.ContentType?.MediaType;

    public async Task<byte[]> ReadAsByteArrayAsync()
    {
        return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
    }

    public async Task<string> ReadAsStringAsync()
    {
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
    }
}