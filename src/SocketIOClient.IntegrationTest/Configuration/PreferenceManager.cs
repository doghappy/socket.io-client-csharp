using System.IO;
using YamlDotNet.Serialization;

namespace SocketIOClient.IntegrationTest.Configuration
{
    public static class PreferenceManager
    {
        private const string defaultConfigFileName = "Configuration/preference.default.yml";
        private const string userConfigFileName = "preference.user.yml";

        public static Preference Get()
        {
            string text;
            if (File.Exists(userConfigFileName))
            {
                text = File.ReadAllText(userConfigFileName);
            }
            else if (File.Exists(defaultConfigFileName))
            {
                text = File.ReadAllText(defaultConfigFileName);
            }
            else
            {
                return new Preference();
            }
            var deserializer = new DeserializerBuilder().Build();
            return deserializer.Deserialize<Preference>(text);
        }
    }
}
