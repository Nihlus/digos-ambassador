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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Services.Feedback
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
		public async Task SendConfirmationAsync([NotNull] SocketCommandContext context, [NotNull] string contents)
		{
			await SendEmbedAsync(context, Color.DarkPurple, contents);
		}

		/// <summary>
		/// Send a negative error message.
		/// </summary>
		/// <param name="context">The context to send to.</param>
		/// <param name="contents">The contents of the message.</param>
		public async Task SendErrorAsync([NotNull] SocketCommandContext context, [NotNull] string contents)
		{
			await SendEmbedAsync(context, Color.Red, contents);
		}

		/// <summary>
		/// Send an alerting warning message.
		/// </summary>
		/// <param name="context">The context to send to.</param>
		/// <param name="contents">The contents of the message.</param>
		public async Task SendWarningAsync([NotNull] SocketCommandContext context, [NotNull] string contents)
		{
			await SendEmbedAsync(context, Color.Orange, contents);
		}

		/// <summary>
		/// Send an informational message.
		/// </summary>
		/// <param name="context">The context to send to.</param>
		/// <param name="contents">The contents of the message.</param>
		public async Task SendInfoAsync([NotNull] SocketCommandContext context, [NotNull] string contents)
		{
			await SendEmbedAsync(context, Color.Blue, contents);
		}

		/// <summary>
		/// Sends an embed.
		/// </summary>
		/// <param name="context">The context of the send operation.</param>
		/// <param name="eb">The embed to send.</param>
		public async Task SendEmbedAsync([NotNull] SocketCommandContext context, [NotNull] EmbedBuilder eb)
		{
			await context.Channel.SendMessageAsync(string.Empty, false, eb);
		}

		private async Task SendEmbedAsync([NotNull] SocketCommandContext context, Color color, [NotNull] string contents)
		{
			var eb = CreateFeedbackEmbed(context.Message.Author, color, contents);
			await SendEmbedAsync(context, eb);
		}

		/// <summary>
		/// Creates a feedback embed.
		/// </summary>
		/// <param name="invoker">The invoking mentionable.</param>
		/// <param name="color">The colour of the embed.</param>
		/// <param name="contents">The contents of the embed.</param>
		/// <returns>A feedback embed.</returns>
		[NotNull]
		public EmbedBuilder CreateFeedbackEmbed([NotNull] IMentionable invoker, Color color, [NotNull] string contents)
		{
			var eb = new EmbedBuilder();
			eb.WithColor(color);
			eb.WithDescription($"{invoker.Mention} | {contents}");

			return eb;
		}

		/// <summary>
		/// Creates an embed that verbosely describes a set of commands.
		/// </summary>
		/// <param name="matchingCommands">A set of commands that should be included in the embed.</param>
		/// <returns>An embed.</returns>
		[NotNull]
		public EmbedBuilder CreateCommandUsageEmbed([NotNull] IReadOnlyList<CommandMatch> matchingCommands)
		{
			var eb = new EmbedBuilder();
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle("Perhaps you meant one of the following?");

			foreach (var matchingCommand in matchingCommands)
			{
				eb.AddField($"{matchingCommand.Alias}", BuildParameterList(matchingCommand.Command));
			}

			return eb;
		}

		/// <summary>
		/// Builds a human-readable parameter list for a command.
		/// </summary>
		/// <param name="commandInfo">The command to get the parameters from.</param>
		/// <returns>A humanized parameter list.</returns>
		[NotNull]
		public string BuildParameterList([NotNull] CommandInfo commandInfo)
		{
			return string.Join
			(
				", ",
				commandInfo.Parameters.Select
				(
					p =>
					{
						var parameterInfo = $"{p.Type.Humanize()} {p.Name}";
						if (p.IsOptional)
						{
							parameterInfo = $"[{parameterInfo}]";
						}

						return parameterInfo;
					}
				)
			);
		}
	}
}
