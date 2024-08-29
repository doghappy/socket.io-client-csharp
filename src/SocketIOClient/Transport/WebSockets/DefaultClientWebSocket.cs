﻿using System;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

#if NET461_OR_GREATER
using System.Reflection;
using System.Collections.Generic;
#endif

namespace SocketIOClient.Transport.WebSockets
{
    public class DefaultClientWebSocket : IClientWebSocket
    {
        public DefaultClientWebSocket(RemoteCertificateValidationCallback remoteCertificateValidationCallback)
        {
            _ws = new ClientWebSocket();
#if NET6_0_OR_GREATER
            _ws.Options.RemoteCertificateValidationCallback = remoteCertificateValidationCallback;
#endif
#if NET461_OR_GREATER
            AllowHeaders();
            ServicePointManager.ServerCertificateValidationCallback = remoteCertificateValidationCallback;
#endif
        }

#if NET461_OR_GREATER
        private static readonly HashSet<string> allowedHeaders = new HashSet<string>
        {
            "User-Agent"
        };

        private void AllowHeaders()
        {
            var property = _ws.Options
                .GetType()
                .GetProperty("RequestHeaders", BindingFlags.NonPublic | BindingFlags.Instance);
            var headers = property.GetValue(_ws.Options);
            var hinfoField =
 headers.GetType().GetField("HInfo", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var hinfo = hinfoField.GetValue(null);
            var hhtField =
 hinfo.GetType().GetField("HeaderHashTable", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            var hashTable = hhtField.GetValue(null) as System.Collections.Hashtable;

            foreach (string key in hashTable.Keys)
            {
                if (!allowedHeaders.Contains(key))
                {
                    continue;
                }
                var headerInfo = hashTable[key];
                foreach (var item in headerInfo.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
                {

                    if (item.Name == "IsRequestRestricted")
                    {
                        bool isRequestRestricted = (bool)item.GetValue(headerInfo);
                        if (isRequestRestricted)
                        {
                            item.SetValue(headerInfo, false);
                        }

                    }
                }

            }
        }
#endif

        private readonly ClientWebSocket _ws;

        public WebSocketState State => (WebSocketState)_ws.State;


        public async Task ConnectAsync(Uri uri, CancellationToken cancellationToken)
        {
            await _ws.ConnectAsync(uri, cancellationToken).ConfigureAwait(false);
        }

        public async Task DisconnectAsync(CancellationToken cancellationToken)
        {
            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task SendAsync(byte[] bytes, TransportMessageType type, bool endOfMessage,
            CancellationToken cancellationToken)
        {
            var msgType = WebSocketMessageType.Text;
            if (type == TransportMessageType.Binary)
            {
                msgType = WebSocketMessageType.Binary;
            }

            await _ws.SendAsync(new ArraySegment<byte>(bytes), msgType, endOfMessage, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<WebSocketReceiveResult> ReceiveAsync(int bufferSize, CancellationToken cancellationToken)
        {
            var buffer = new byte[bufferSize];
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken)
                .ConfigureAwait(false);
            return new WebSocketReceiveResult
            {
                Count = result.Count,
                MessageType = (TransportMessageType)result.MessageType,
                EndOfMessage = result.EndOfMessage,
                Buffer = buffer,
            };
        }

        public void AddHeader(string key, string val)
        {
            _ws.Options.SetRequestHeader(key, val);
        }

        public void SetProxy(IWebProxy proxy) => _ws.Options.Proxy = proxy;

        public void Dispose()
        {
            _ws.Dispose();
        }
    }
}