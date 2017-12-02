//
//  InteractiveServiceExtensions.cs
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

using System.Threading.Tasks;

using DIGOS.Ambassador.Pagination;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

namespace DIGOS.Ambassador.Extensions
{
	/// <summary>
	/// Contains extensions to the <see cref="InteractiveService"/> class.
	/// </summary>
	public static class InteractiveServiceExtensions
	{
		/// <summary>
		/// Sends a paginated message to the context user's direct messaging channel, alerting them if they are
		/// not already in it.
		/// </summary>
		/// <param name="this">The interactive service.</param>
		/// <param name="context">The command context.</param>
		/// <param name="feedback">The feedback service to use.</param>
		/// <param name="pager">The pager to send.</param>
		/// <param name="criterion">The reaction criterion.</param>
		/// <typeparam name="T1">The type of content in the pager.</typeparam>
		/// <typeparam name="T2">The type of pager.</typeparam>
		/// <returns>The message that was sent.</returns>
		public static async Task<IUserMessage> SendPrivatePaginatedMessageAsync<T1, T2>
		(
			this InteractiveService @this,
			SocketCommandContext context,
			UserFeedbackService feedback,
			IPager<T1, T2> pager,
			ICriterion<SocketReaction> criterion = null
		)
			where T2 : IPager<T1, T2>
		{
			if (!context.IsPrivate)
			{
				await feedback.SendConfirmationAsync(context, "Please check your private messages.");
			}

			var userChannel = await context.User.GetOrCreateDMChannelAsync();
			return await SendPaginatedMessageAsync(@this, context, feedback, pager, userChannel, criterion);
		}

		/// <summary>
		/// Sends a paginated message to the specified channel.
		/// </summary>
		/// <param name="this">The interactive service.</param>
		/// <param name="context">The command context.</param>
		/// <param name="feedback">The feedback service to use.</param>
		/// <param name="pager">The pager to send.</param>
		/// <param name="channel">The channel to send the pager to.</param>
		/// <param name="criterion">The reaction criterion.</param>
		/// <typeparam name="T1">The type of content in the pager.</typeparam>
		/// <typeparam name="T2">The type of pager.</typeparam>
		/// <returns>The message that was sent.</returns>
		public static async Task<IUserMessage> SendPaginatedMessageAsync<T1, T2>
		(
			this InteractiveService @this,
			SocketCommandContext context,
			UserFeedbackService feedback,
			IPager<T1, T2> pager,
			IMessageChannel channel = null,
			ICriterion<SocketReaction> criterion = null
		)
		where T2 : IPager<T1, T2>
		{
			var callback = new PaginatedCallback<T1, T2>(@this, feedback, context, pager, channel, criterion);
			await callback.DisplayAsync().ConfigureAwait(false);

			return callback.Message;
		}
	}
}
