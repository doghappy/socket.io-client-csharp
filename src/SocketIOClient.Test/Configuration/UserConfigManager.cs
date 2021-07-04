using System.IO;

namespace SocketIOClient.Test.Configuration
{
    public class UserConfigManager
    {
        private const string configFileName = "user.json";

        public bool Exists =>  File.Exists(configFileName);

        public UserConfig Get()
        {
            if (!this.Exists) 
            {
                return null;
            }

            return System.Text.Json.JsonSerializer.Deserialize<UserConfig>(File.ReadAllText(configFileName));
        }
    }
}
