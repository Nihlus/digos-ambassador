//
//  WebSocketSharpProvider.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net;
using Discord.Net.WebSockets;
using JetBrains.Annotations;
using WebSocketSharp;

namespace DIGOS.Ambassador.Utility
{
    /// <summary>
    /// WebSocket provider using websocket-sharp.
    /// </summary>
    public class WebSocketSharpProvider : IWebSocketClient
    {
        /// <inheritdoc />
        public event Func<byte[], int, int, Task> BinaryMessage;

        /// <inheritdoc />
        public event Func<string, Task> TextMessage;

        /// <inheritdoc />
        public event Func<Exception, Task> Closed;

        private readonly SemaphoreSlim _lock;
        private readonly Dictionary<string, string> _headers;
        private readonly ManualResetEventSlim _waitUntilConnect;

        private WebSocket _client;
        private CancellationTokenSource _cancelTokenSource;
        private CancellationToken _cancelToken;
        private CancellationToken _parentToken;

        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketSharpProvider"/> class.
        /// </summary>
        public WebSocketSharpProvider()
        {
            _headers = new Dictionary<string, string>();
            _lock = new SemaphoreSlim(1, 1);
            _cancelTokenSource = new CancellationTokenSource();
            _cancelToken = CancellationToken.None;
            _parentToken = CancellationToken.None;
            _waitUntilConnect = new ManualResetEventSlim();
        }

        /// <inheritdoc />
        public void SetHeader([NotNull] string key, string value)
        {
            _headers[key] = value;
        }

        /// <inheritdoc />
        public void SetCancelToken(CancellationToken cancelToken)
        {
            _parentToken = cancelToken;
            _cancelToken = CancellationTokenSource.CreateLinkedTokenSource
            (
                _parentToken,
                _cancelTokenSource.Token
            )
            .Token;
        }

        /// <inheritdoc />
        public async Task ConnectAsync(string host)
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await ConnectInternalAsync(host).ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        private async Task ConnectInternalAsync(string host)
        {
            await DisconnectInternalAsync().ConfigureAwait(false);

            _cancelTokenSource = new CancellationTokenSource();
            _cancelToken = CancellationTokenSource.CreateLinkedTokenSource
                (
                    _parentToken,
                    _cancelTokenSource.Token
                )
                .Token;

            _client = new WebSocket(host)
            {
                CustomHeaders = _headers.ToList()
            };
            _client.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12;

            _client.OnMessage += OnMessage;
            _client.OnOpen += OnConnected;
            _client.OnClose += OnClosed;

            _client.Connect();
            _waitUntilConnect.Wait(_cancelToken);
        }

        /// <inheritdoc />
        public async Task DisconnectAsync()
        {
            await _lock.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisconnectInternalAsync().ConfigureAwait(false);
            }
            finally
            {
                _lock.Release();
            }
        }

        [NotNull]
        private Task DisconnectInternalAsync()
        {
            _cancelTokenSource.Cancel();
            if (_client is null)
            {
                return Task.CompletedTask;
            }

            if (_client.ReadyState == WebSocketState.Open)
            {
                _client.Close();
            }

            _client.OnMessage -= OnMessage;
            _client.OnOpen -= OnConnected;
            _client.OnClose -= OnClosed;

            _client = null;
            _waitUntilConnect.Reset();

            return Task.CompletedTask;
        }

        private void OnMessage(object sender, [NotNull] MessageEventArgs messageEventArgs)
        {
            if (messageEventArgs.IsBinary)
            {
                OnBinaryMessage(messageEventArgs);
            }
            else if (messageEventArgs.IsText)
            {
                OnTextMessage(messageEventArgs);
            }
        }

        /// <inheritdoc />
        public async Task SendAsync([NotNull] byte[] data, int index, int count, bool isText)
        {
            await _lock.WaitAsync(_cancelToken).ConfigureAwait(false);
            try
            {
                if (isText)
                {
                    _client.Send(Encoding.UTF8.GetString(data, index, count));
                }
                else
                {
                    _client.Send(data.Skip(index).Take(count).ToArray());
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        private void OnTextMessage(MessageEventArgs e)
        {
            this.TextMessage?.Invoke(e.Data).GetAwaiter().GetResult();
        }

        private void OnBinaryMessage(MessageEventArgs e)
        {
            this.BinaryMessage?.Invoke(e.RawData, 0, e.RawData.Length).GetAwaiter().GetResult();
        }

        private void OnConnected(object sender, EventArgs e)
        {
            _waitUntilConnect.Set();
        }

        private void OnClosed(object sender, [NotNull] CloseEventArgs e)
        {
            if (e.WasClean)
            {
                this.Closed?.Invoke(null).GetAwaiter().GetResult();
                return;
            }

            var ex = new WebSocketClosedException(e.Code, e.Reason);
            this.Closed?.Invoke(ex).GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            DisconnectInternalAsync().GetAwaiter().GetResult();

            ((IDisposable)_client)?.Dispose();
            _client = null;

            _isDisposed = true;
        }
    }
}
