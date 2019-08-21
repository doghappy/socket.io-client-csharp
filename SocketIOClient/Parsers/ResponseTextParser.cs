using System.Threading.Tasks;

namespace SocketIOClient.Parsers
{
    public class ResponseTextParser
    {
        public ResponseTextParser(string ns, SocketIO socket)
        {
            Parser = new OpenedParser();
            Namespace = ns;
            Socket = socket;
        }

        public IParser Parser { get; set; }
        public string Text { get; set; }
        public string Namespace { get; }
        public SocketIO Socket { get; }

        public Task ParseAsync() => Parser.ParseAsync(this);
    }
}
