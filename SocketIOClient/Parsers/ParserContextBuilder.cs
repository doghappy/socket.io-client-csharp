using System;
using System.Linq;

namespace SocketIOClient.Parsers
{
    class ParserContextBuilder
    {
        public ParserContextBuilder(string uri, ParserContext ctx) : this(new Uri(uri), ctx) { }
        public ParserContextBuilder(Uri uri, ParserContext ctx)
        {
            _ctx = ctx;
            _protocols = new[] { "https", "http", "wss", "ws" };
            BuildUri(uri);
            UrlConverter = new UrlConverter();
        }

        readonly ParserContext _ctx;
        readonly string[] _protocols;

        public UrlConverter UrlConverter { get; set; }

        private void BuildUri(Uri uri)
        {
            if (_protocols.Contains(uri.Scheme))
            {
                _ctx.Uri = uri;
            }
            else
            {
                throw new ArgumentException("Unsupported protocol");
            }
        }

        private void BuildWsUri()
        {
            _ctx.WsUri = UrlConverter.HttpToWs(_ctx.Uri, _ctx.Path, _ctx.Parameters);
        }

        private void BuildNamespace()
        {
            if (_ctx.Uri.AbsolutePath != "/")
            {
                _ctx.Namespace = _ctx.Uri.AbsolutePath + ',';
            }
        }

        public ParserContext Build()
        {
            BuildNamespace();
            BuildWsUri();
            return _ctx;
        }
    }
}
