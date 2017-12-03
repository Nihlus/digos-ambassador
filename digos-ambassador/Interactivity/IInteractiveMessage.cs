//
//  IInteractiveMessage.cs
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
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Interactivity
{
	/// <summary>
	/// Interface for interactive messages.
	/// </summary>
	public interface IInteractiveMessage
	{
		/// <summary>
		/// Gets the underlying message.
		/// </summary>
		IUserMessage Message { get; }

		/// <summary>
		/// Gets the reaction callback for the message, if any.
		/// </summary>
		[CanBeNull]
		IReactionCallback ReactionCallback { get; }

		/// <summary>
		/// Gets the timeout after which the message should be deleted, if any.
		/// </summary>
		TimeSpan? Timeout { get; }

		/// <summary>
		/// Displays the message in the given channel.
		/// </summary>
		/// <param name="channel">The channel to send the message to.</param>
		/// <returns>A user message.</returns>
		Task<IUserMessage> DisplayAsync(ISocketMessageChannel channel);
	}
}
