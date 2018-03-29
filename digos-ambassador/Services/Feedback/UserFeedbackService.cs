//
//  UserFeedbackService.cs
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
using System.Threading.Tasks;

using DIGOS.Ambassador.Extensions;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;

using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Handles sending formatted messages to the users.
	/// </summary>
	public class UserFeedbackService
	{
		/// <summary>
		/// Sends an error message, and deletes it after a specified timeout.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="interactivity">The interactivity service.</param>
		/// <param name="contents">The contents of the message.</param>
		/// <param name="timeout">The timeout after which the message should be deleted.</param>
		public async Task SendErrorAndDeleteAsync
		(
			[NotNull] SocketCommandContext context,
			[NotNull] InteractiveService interactivity,
			[NotNull] string contents,
			[CanBeNull] TimeSpan? timeout = null
		)
		{
			await SendEmbedAndDeleteAsync(context, interactivity, Color.Red, contents, timeout);
		}

		/// <summary>
		/// Sends a warning message, and deletes it after a specified timeout.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="interactivity">The interactivity service.</param>
		/// <param name="contents">The contents of the message.</param>
		/// <param name="timeout">The timeout after which the message should be deleted.</param>
		public async Task SendWarningAndDeleteAsync
		(
			[NotNull] SocketCommandContext context,
			[NotNull] InteractiveService interactivity,
			[NotNull] string contents,
			[CanBeNull] TimeSpan? timeout = null
		)
		{
			await SendEmbedAndDeleteAsync(context, interactivity, Color.Orange, contents, timeout);
		}

		/// <summary>
		/// Sends a confirmation message, and deletes it after a specified timeout.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="interactivity">The interactivity service.</param>
		/// <param name="contents">The contents of the message.</param>
		/// <param name="timeout">The timeout after which the message should be deleted.</param>
		public async Task SendConfirmationAndDeleteAsync
		(
			[NotNull] SocketCommandContext context,
			[NotNull] InteractiveService interactivity,
			[NotNull] string contents,
			[CanBeNull] TimeSpan? timeout = null
		)
		{
			await SendEmbedAndDeleteAsync(context, interactivity, Color.DarkPurple, contents, timeout);
		}

		/// <summary>
		/// Sends an embed, and deletes it after a specified timeout.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="interactivity">The interactivity service.</param>
		/// <param name="colour">The colour of the embed.</param>
		/// <param name="contents">The contents of the message.</param>
		/// <param name="timeout">The timeout after which the message should be deleted.</param>
		public async Task SendEmbedAndDeleteAsync
		(
			[NotNull] SocketCommandContext context,
			[NotNull] InteractiveService interactivity,
			Color colour,
			[NotNull] string contents,
			[CanBeNull] TimeSpan? timeout = null
		)
		{
			var eb = CreateFeedbackEmbed(context.User, colour, contents);
			await interactivity.ReplyAndDeleteAsync(context, string.Empty, false, eb, timeout);
		}

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
		/// Sends a private embed to a given user, alerting them in their current context if they're not already in a
		/// DM.
		/// </summary>
		/// <param name="context">The context of the command.</param>
		/// <param name="user">The user to send the embed to.</param>
		/// <param name="eb">The embed to send.</param>
		/// <param name="notify">Whether or not to notify the user that they've been sent a message.</param>
		public async Task SendPrivateEmbedAsync([NotNull] SocketCommandContext context, IUser user, Embed eb, bool notify = true)
		{
			await user.SendMessageAsync(string.Empty, false, eb);

			if (!context.IsPrivate && notify)
			{
				await SendConfirmationAsync(context, "Please check your private messages.");
			}
		}

		/// <summary>
		/// Creates a feedback embed.
		/// </summary>
		/// <param name="invoker">The invoking mentionable.</param>
		/// <param name="color">The colour of the embed.</param>
		/// <param name="contents">The contents of the embed.</param>
		/// <returns>A feedback embed.</returns>
		[Pure]
		[NotNull]
		public EmbedBuilder CreateFeedbackEmbed([NotNull] IMentionable invoker, Color color, [NotNull] string contents)
		{
			var eb = CreateBaseEmbed(color);
			eb.WithDescription($"{invoker.Mention} | {contents}");

			return eb;
		}

		/// <summary>
		/// Creates a base embed.
		/// </summary>
		/// <param name="color">The colour of the embed. Optional.</param>
		/// <returns>A basic embed.</returns>
		[Pure]
		[NotNull]
		public EmbedBuilder CreateBaseEmbed(Color? color = null)
		{
			color = color ?? Color.DarkPurple;

			var eb = new EmbedBuilder();
			eb.WithColor(color.Value);

			return eb;
		}

		/// <summary>
		/// Creates an embed that verbosely describes a set of commands.
		/// </summary>
		/// <param name="matchingCommands">A set of commands that should be included in the embed.</param>
		/// <returns>An embed.</returns>
		[Pure]
		[NotNull]
		public EmbedBuilder CreateCommandUsageEmbed([NotNull] IReadOnlyList<CommandMatch> matchingCommands)
		{
			var eb = CreateBaseEmbed();
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
		[Pure]
		[NotNull]
		public string BuildParameterList([NotNull] CommandInfo commandInfo)
		{
			var result = string.Join
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

			if (result.IsNullOrWhitespace())
			{
				return "No parameters";
			}

			return result;
		}
	}
}
