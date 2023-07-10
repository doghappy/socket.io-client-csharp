using Microsoft.Extensions.Configuration;

namespace SocketIOClient.IntegrationTests.Common
{
    public class Startup
    {
        public static IConfiguration Configuration { get; private set; }

        public static string V4_WS { get; private set; }
        public static string V4_NSP_WS { get; private set; }
        public static string V4_WS_TOKEN { get; private set; }
        public static string V4_NSP_WS_TOKEN { get; private set; }

        public static string V4_HTTP { get; private set; }
        public static string V4_NSP_HTTP { get; private set; }
        public static string V4_HTTP_TOKEN { get; private set; }
        public static string V4_NSP_HTTP_TOKEN { get; private set; }

        public static string V4_WS_MP { get; private set; }
        public static string V4_NSP_WS_MP { get; private set; }
        public static string V4_WS_TOKEN_MP { get; private set; }
        public static string V4_NSP_WS_TOKEN_MP { get; private set; }

        public static string V4_HTTP_MP { get; private set; }
        public static string V4_NSP_HTTP_MP { get; private set; }
        public static string V4_HTTP_TOKEN_MP { get; private set; }
        public static string V4_NSP_HTTP_TOKEN_MP { get; private set; }

        public static string V2_WS { get; private set; }
        public static string V2_NSP_WS { get; private set; }
        public static string V2_WS_TOKEN { get; private set; }
        public static string V2_NSP_WS_TOKEN { get; private set; }

        public static string V2_HTTP { get; private set; }
        public static string V2_NSP_HTTP { get; private set; }
        public static string V2_HTTP_TOKEN { get; private set; }
        public static string V2_NSP_HTTP_TOKEN { get; private set; }

        public static string V2_WS_MP { get; private set; }
        public static string V2_NSP_WS_MP { get; private set; }
        public static string V2_WS_TOKEN_MP { get; private set; }
        public static string V2_NSP_WS_TOKEN_MP { get; private set; }

        public static string V2_HTTP_MP { get; private set; }
        public static string V2_NSP_HTTP_MP { get; private set; }
        public static string V2_HTTP_TOKEN_MP { get; private set; }
        public static string V2_NSP_HTTP_TOKEN_MP { get; private set; }

        public static void Initialize()
        {
            Configuration = new ConfigurationBuilder()
                .AddYamlFile("appsettings.yml")
                .AddYamlFile("appsettings.user.yml", true)
                .AddEnvironmentVariables()
                // .AddCommandLine(args)
                .Build();
            SetUrl(Configuration);
        }

        private static void SetUrl(IConfiguration configuration)
        {
            V4_WS = configuration["server:v4:ws"];
            V4_NSP_WS = configuration["server:v4:nsp_ws"];
            V4_WS_TOKEN = configuration["server:v4:ws_token"];
            V4_NSP_WS_TOKEN = configuration["server:v4:nsp_ws_token"];

            V4_HTTP = configuration["server:v4:http"];
            V4_NSP_HTTP = configuration["server:v4:nsp_http"];
            V4_HTTP_TOKEN = configuration["server:v4:http_token"];
            V4_NSP_HTTP_TOKEN = configuration["server:v4:nsp_http_token"];
            
            V4_WS_MP = configuration["server:v4_mp:ws"];
            V4_NSP_WS_MP = configuration["server:v4_mp:nsp_ws"];
            V4_WS_TOKEN_MP = configuration["server:v4_mp:ws_token"];
            V4_NSP_WS_TOKEN_MP = configuration["server:v4_mp:nsp_ws_token"];

            V4_HTTP_MP = configuration["server:v4_mp:http"];
            V4_NSP_HTTP_MP = configuration["server:v4_mp:nsp_http"];
            V4_HTTP_TOKEN_MP = configuration["server:v4_mp:http_token"];
            V4_NSP_HTTP_TOKEN_MP = configuration["server:v4_mp:nsp_http_token"];

            V2_WS = configuration["server:v2:ws"];
            V2_NSP_WS = configuration["server:v2:nsp_ws"];
            V2_WS_TOKEN = configuration["server:v2:ws_token"];
            V2_NSP_WS_TOKEN = configuration["server:v2:nsp_ws_token"];

            V2_HTTP = configuration["server:v2:http"];
            V2_NSP_HTTP = configuration["server:v2:nsp_ws"];
            V2_HTTP_TOKEN = configuration["server:v2:ws_token"];
            V2_NSP_HTTP_TOKEN = configuration["server:v2:nsp_ws_token"];
            

            V2_WS_MP = configuration["server:v2_mp:ws"];
            V2_NSP_WS_MP = configuration["server:v2_mp:nsp_ws"];
            V2_WS_TOKEN_MP = configuration["server:v2_mp:ws_token"];
            V2_NSP_WS_TOKEN_MP = configuration["server:v2_mp:nsp_ws_token"];

            V2_HTTP_MP = configuration["server:v2_mp:http"];
            V2_NSP_HTTP_MP = configuration["server:v2_mp:nsp_ws"];
            V2_HTTP_TOKEN_MP = configuration["server:v2_mp:ws_token"];
            V2_NSP_HTTP_TOKEN_MP = configuration["server:v2_mp:nsp_ws_token"];
        }
    }
}