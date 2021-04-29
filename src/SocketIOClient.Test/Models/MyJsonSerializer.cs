using SocketIOClient.JsonSerializer;
using System.Text.Json;

namespace SocketIOClient.Test.Models
{
    class MyJsonSerializer : SystemTextJsonSerializer
    {
        public MyJsonSerializer(int eio) : base(eio)
        {

        }

        public override JsonSerializerOptions CreateOptions()
        {
            var options = base.CreateOptions();
            options.PropertyNameCaseInsensitive = true;
            return options;
        }
    }
}
