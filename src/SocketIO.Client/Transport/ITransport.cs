﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using SocketIO.Core;
using SocketIO.Serializer.Core;

namespace SocketIO.Client.Transport
{
    public interface ITransport : IDisposable
    {
        Action<IMessage> OnReceived { get; set; }
        Action<Exception> OnError { get; set; }
        string Namespace { get; set; }
        Task SendAsync(IList<SerializedItem> items, CancellationToken cancellationToken);
        Task ConnectAsync(Uri uri, CancellationToken cancellationToken);
        Task DisconnectAsync(CancellationToken cancellationToken);
        void AddHeader(string key, string val);
        void SetProxy(IWebProxy proxy);
    }
}