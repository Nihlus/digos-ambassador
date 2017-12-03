//
//  InteractiveMessage.cs
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
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace DIGOS.Ambassador.Interactivity
{
	/// <summary>
	/// Represents an interactive message.
	/// </summary>
	public abstract class InteractiveMessage : IInteractiveMessage
	{
		/// <summary>
		/// Gets the context of the message.
		/// </summary>
		public SocketCommandContext Context { get; }

		/// <summary>
		/// Gets the interacivity service.
		/// </summary>
		protected InteractiveService Interactive { get; }

		/// <inheritdoc />
		public IUserMessage Message { get; protected set; }

		/// <inheritdoc />
		public IReactionCallback ReactionCallback { get; protected set; }

		/// <inheritdoc />
		public TimeSpan? Timeout { get; protected set; }

		/// <inheritdoc />
		public abstract Task<IUserMessage> DisplayAsync(ISocketMessageChannel socketMessageChannel);

		/// <summary>
		/// Initializes a new instance of the <see cref="InteractiveMessage"/> class.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="interactiveService">The interactive service.</param>
		protected InteractiveMessage(SocketCommandContext context, InteractiveService interactiveService)
		{
			this.Context = context;
			this.Interactive = interactiveService;
		}
	}
}
