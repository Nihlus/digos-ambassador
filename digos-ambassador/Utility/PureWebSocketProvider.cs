//
//  PureWebSocketProvider.cs
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
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord.Net;
using Discord.Net.WebSockets;
using Humanizer;
using PureWebSockets;

namespace DIGOS.Ambassador.Utility
{
	/// <summary>
	/// WebSocket provider using PureWebSocket.
	/// </summary>
	public class PureWebSocketProvider : IWebSocketClient, IDisposable
	{
		/// <inheritdoc />
		public event Func<byte[], int, int, Task> BinaryMessage;

		/// <inheritdoc />
		public event Func<string, Task> TextMessage;

		/// <inheritdoc />
		public event Func<Exception, Task> Closed;

		private readonly SemaphoreSlim Lock;
		private readonly Dictionary<string, string> Headers;
		private readonly ManualResetEventSlim WaitUntilConnect;

		private PureWebSocket Client;
		private CancellationTokenSource CancelTokenSource;
		private CancellationToken CancelToken;
		private CancellationToken ParentToken;

		private bool IsDisposed;

		/// <summary>
		/// Initializes a new instance of the <see cref="PureWebSocketProvider"/> class.
		/// </summary>
		public PureWebSocketProvider()
		{
			this.Headers = new Dictionary<string, string>();
			this.Lock = new SemaphoreSlim(1, 1);
			this.CancelTokenSource = new CancellationTokenSource();
			this.CancelToken = CancellationToken.None;
			this.ParentToken = CancellationToken.None;
			this.WaitUntilConnect = new ManualResetEventSlim();
		}

		/// <inheritdoc />
		public void SetHeader(string key, string value)
		{
			this.Headers[key] = value;
		}

		/// <inheritdoc />
		public void SetCancelToken(CancellationToken cancelToken)
		{
			this.ParentToken = cancelToken;
			this.CancelToken = CancellationTokenSource.CreateLinkedTokenSource
			(
				this.ParentToken,
				this.CancelTokenSource.Token
			)
			.Token;
		}

		/// <inheritdoc />
		public async Task ConnectAsync(string host)
		{
			await this.Lock.WaitAsync().ConfigureAwait(false);
			try
			{
				await ConnectInternalAsync(host).ConfigureAwait(false);
			}
			finally
			{
				this.Lock.Release();
			}
		}

		private async Task ConnectInternalAsync(string host)
		{
			await DisconnectInternalAsync().ConfigureAwait(false);

			this.CancelTokenSource = new CancellationTokenSource();
			this.CancelToken = CancellationTokenSource.CreateLinkedTokenSource
				(
					this.ParentToken,
					this.CancelTokenSource.Token
				)
				.Token;

			this.Client = new PureWebSocket(host)
			{
				RequestHeaders = this.Headers.ToList().Select(kvp => (kvp.Key, kvp.Value).ToTuple()).ToArray(),
			};

			this.Client.OnMessage += OnMessage;
			this.Client.OnData += OnData;
			this.Client.OnOpened += OnConnected;
			this.Client.OnClosed += OnClosed;

			this.Client.Connect();
			this.WaitUntilConnect.Wait(this.CancelToken);
		}

		/// <inheritdoc />
		public async Task DisconnectAsync()
		{
			await this.Lock.WaitAsync().ConfigureAwait(false);
			try
			{
				await DisconnectInternalAsync().ConfigureAwait(false);
			}
			finally
			{
				this.Lock.Release();
			}
		}

		private Task DisconnectInternalAsync()
		{
			this.CancelTokenSource.Cancel();
			if (this.Client is null)
			{
				return Task.CompletedTask;
			}

			if (this.Client.State == WebSocketState.Open)
			{
				this.Client.Disconnect();
			}

			this.Client.OnMessage -= OnMessage;
			this.Client.OnOpened -= OnConnected;
			this.Client.OnClosed -= OnClosed;

			this.Client = null;
			this.WaitUntilConnect.Reset();

			return Task.CompletedTask;
		}

		private void OnMessage(string message)
		{
			this.TextMessage?.Invoke(message).GetAwaiter().GetResult();
		}

		private void OnData(byte[] data)
		{
			this.BinaryMessage?.Invoke(data, 0, data.Length).GetAwaiter().GetResult();
		}

		/// <inheritdoc />
		public async Task SendAsync(byte[] data, int index, int count, bool isText)
		{
			await this.Lock.WaitAsync(this.CancelToken).ConfigureAwait(false);
			try
			{
				if (isText)
				{
					this.Client.Send(Encoding.UTF8.GetString(data, index, count));
				}
				else
				{
					var asString = Encoding.ASCII.GetString(data, index, count);
					this.Client.Send(asString);
				}
			}
			finally
			{
				this.Lock.Release();
			}
		}

		private void OnConnected()
		{
			this.WaitUntilConnect.Set();
		}

		private void OnClosed(WebSocketCloseStatus reason)
		{
			if (reason == WebSocketCloseStatus.NormalClosure)
			{
				this.Closed?.Invoke(null).GetAwaiter().GetResult();
				return;
			}

			var ex = new WebSocketClosedException((int)reason, reason.Humanize());
			this.Closed?.Invoke(ex).GetAwaiter().GetResult();
		}

		/// <inheritdoc />
		public void Dispose()
		{
			if (this.IsDisposed)
			{
				return;
			}

			DisconnectInternalAsync().GetAwaiter().GetResult();

			((IDisposable)this.Client)?.Dispose();
			this.Client = null;

			this.IsDisposed = true;
		}
	}
}
