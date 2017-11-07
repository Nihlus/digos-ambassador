//
//  UserFeedbackService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Handles sending formatted messages to the users.
	/// </summary>
	public class UserFeedbackService
	{
		/// <summary>
		/// Send a positive confirmation message.
		/// </summary>
		/// <param name="context">The context to send to.</param>
		/// <param name="contents">The contents of the message.</param>
		public async Task SendConfirmationAsync(SocketCommandContext context, string contents)
		{
			await SendEmbedAsync(context, Color.DarkPurple, contents);
		}

		/// <summary>
		/// Send a negative error message.
		/// </summary>
		/// <param name="context">The context to send to.</param>
		/// <param name="contents">The contents of the message.</param>
		public async Task SendErrorAsync(SocketCommandContext context, string contents)
		{
			await SendEmbedAsync(context, Color.Red, contents);
		}

		/// <summary>
		/// Send an alerting warning message.
		/// </summary>
		/// <param name="context">The context to send to.</param>
		/// <param name="contents">The contents of the message.</param>
		public async Task SendWarningAsync(SocketCommandContext context, string contents)
		{
			await SendEmbedAsync(context, Color.Orange, contents);
		}

		/// <summary>
		/// Send an informational message.
		/// </summary>
		/// <param name="context">The context to send to.</param>
		/// <param name="contents">The contents of the message.</param>
		public async Task SendInfoAsync(SocketCommandContext context, string contents)
		{
			await SendEmbedAsync(context, Color.Blue, contents);
		}

		private async Task SendEmbedAsync(SocketCommandContext context, Color color, string contents)
		{
			var eb = CreateFeedbackEmbed(context.Message.Author, color, contents);
			await context.Channel.SendMessageAsync(string.Empty, false, eb);
		}

		private EmbedBuilder CreateFeedbackEmbed(IMentionable invoker, Color color, string contents)
		{
			var eb = new EmbedBuilder();
			eb.WithColor(color);
			eb.WithDescription($"{invoker.Mention} | {contents}");

			return eb;
		}
	}
}
