using System.Collections.Generic;
using System.Text;

namespace SocketIOClient.IntegrationTests.Transport.WebSockets
{
    public class TestHelper
    {
        public static readonly List<string> TestMessages = new List<string>
        {
            new string('a', 11),
            new string('酷', 11),
            "😎😎😎😎😎😎😎😎😎😎😎"
            // new string('a', 1024 * 9),
            // new string('酷', 1024 * 9),
            // CreateEmojiString("😎", 1024 * 9),
        };

        static string CreateEmojiString(string emoji, int n)
        {
            var builder = new StringBuilder(n);
            for (int i = 0; i < n; i++)
            {
                builder.Append(emoji);
            }
            return builder.ToString();
        }

    }
}