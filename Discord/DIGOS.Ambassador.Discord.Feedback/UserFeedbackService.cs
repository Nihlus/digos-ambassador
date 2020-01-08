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
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using Humanizer.Bytes;
using JetBrains.Annotations;
using Remora.Behaviours.Services;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Discord.Feedback
{
    /// <summary>
    /// Handles sending formatted messages to the users.
    /// </summary>
    [PublicAPI]
    public class UserFeedbackService
    {
        private readonly DelayedActionService _delayedActions;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserFeedbackService"/> class.
        /// </summary>
        /// <param name="delayedActions">The delayed actions service.</param>
        public UserFeedbackService(DelayedActionService delayedActions)
        {
            _delayedActions = delayedActions;
        }

        /// <summary>
        /// Sends an error message, and deletes it after a specified timeout.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="timeout">The timeout after which the message should be deleted.</param>
        [NotNull]
        public async Task SendErrorAndDeleteAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] string contents,
            TimeSpan? timeout = null
        )
        {
            await SendEmbedAndDeleteAsync(context, Color.Red, contents, timeout);
        }

        /// <summary>
        /// Sends a warning message, and deletes it after a specified timeout.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="timeout">The timeout after which the message should be deleted.</param>
        [NotNull]
        public async Task SendWarningAndDeleteAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] string contents,
            TimeSpan? timeout = null
        )
        {
            await SendEmbedAndDeleteAsync(context, Color.Orange, contents, timeout);
        }

        /// <summary>
        /// Sends a confirmation message, and deletes it after a specified timeout.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="timeout">The timeout after which the message should be deleted.</param>
        [NotNull]
        public async Task SendConfirmationAndDeleteAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] string contents,
            TimeSpan? timeout = null
        )
        {
            await SendEmbedAndDeleteAsync(context, Color.DarkPurple, contents, timeout);
        }

        /// <summary>
        /// Sends an embed, and deletes it after a specified timeout.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="colour">The colour of the embed.</param>
        /// <param name="contents">The contents of the message.</param>
        /// <param name="timeout">The timeout after which the message should be deleted.</param>
        [NotNull]
        public async Task SendEmbedAndDeleteAsync
        (
            [NotNull] ICommandContext context,
            Color colour,
            [NotNull] string contents,
            TimeSpan? timeout = null
        )
        {
            var eb = CreateFeedbackEmbed(context.User, colour, contents);
            await SendEmbedAndDeleteAsync(context.Channel, eb, timeout);
        }

        /// <summary>
        /// Send a positive confirmation message.
        /// </summary>
        /// <param name="context">The context to send to.</param>
        /// <param name="contents">The contents of the message.</param>
        [NotNull]
        public async Task SendConfirmationAsync([NotNull] ICommandContext context, [NotNull] string contents)
        {
            await SendEmbedAsync(context, Color.DarkPurple, contents);
        }

        /// <summary>
        /// Send a negative error message.
        /// </summary>
        /// <param name="context">The context to send to.</param>
        /// <param name="contents">The contents of the message.</param>
        [NotNull]
        public async Task SendErrorAsync([NotNull] ICommandContext context, [NotNull] string contents)
        {
            await SendEmbedAsync(context, Color.Red, contents);
        }

        /// <summary>
        /// Send an alerting warning message.
        /// </summary>
        /// <param name="context">The context to send to.</param>
        /// <param name="contents">The contents of the message.</param>
        [NotNull]
        public async Task SendWarningAsync([NotNull] ICommandContext context, [NotNull] string contents)
        {
            await SendEmbedAsync(context, Color.Orange, contents);
        }

        /// <summary>
        /// Send an informational message.
        /// </summary>
        /// <param name="context">The context to send to.</param>
        /// <param name="contents">The contents of the message.</param>
        [NotNull]
        public async Task SendInfoAsync([NotNull] ICommandContext context, [NotNull] string contents)
        {
            await SendEmbedAsync(context, Color.Blue, contents);
        }

        /// <summary>
        /// Sends an embed.
        /// </summary>
        /// <param name="channel">The context of the send operation.</param>
        /// <param name="eb">The embed to send.</param>
        [NotNull]
        public async Task SendEmbedAsync([NotNull] IMessageChannel channel, [NotNull] Embed eb)
        {
            await channel.SendMessageAsync(string.Empty, false, eb);
        }

        /// <summary>
        /// Sends an embed to the given channel, and deletes it after a certain timeout.
        /// </summary>
        /// <param name="channel">The channel to send the embed to.</param>
        /// <param name="eb">The embed.</param>
        /// <param name="timeout">The timeout after which the embed will be deleted. Defaults to 15 seconds.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        public async Task SendEmbedAndDeleteAsync
        (
            [NotNull] IMessageChannel channel,
            [NotNull] Embed eb,
            TimeSpan? timeout = null
        )
        {
            timeout ??= TimeSpan.FromSeconds(15.0);

            var message = await channel.SendMessageAsync(string.Empty, embed: eb);

            _delayedActions.DelayUntil(() => message.DeleteAsync(), timeout.Value);
        }

        /// <summary>
        /// Sends the given string as one or more sequential embeds, chunked into sets of 1024 characters.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="color">The embed colour.</param>
        /// <param name="contents">The contents to send.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        private async Task SendEmbedAsync([NotNull] ICommandContext context, Color color, [NotNull] string contents)
        {
            // Sometimes the content is > 2048 in length. We'll chunk it into embeds of 1024 here.
            if (contents.Length < 1024)
            {
                var eb = CreateFeedbackEmbed(context.Message.Author, color, contents);
                await SendEmbedAsync(context.Channel, eb);

                return;
            }

            var words = contents.Split(' ');
            var messageBuilder = new StringBuilder();
            foreach (var word in words)
            {
                if (messageBuilder.Length >= 1024)
                {
                    var eb = CreateFeedbackEmbed(context.Message.Author, color, messageBuilder.ToString());
                    await SendEmbedAsync(context.Channel, eb);

                    messageBuilder.Clear();
                }

                messageBuilder.Append(word);
                messageBuilder.Append(" ");
            }

            if (messageBuilder.Length > 0)
            {
                var eb = CreateFeedbackEmbed(context.Message.Author, color, messageBuilder.ToString());
                await SendEmbedAsync(context.Channel, eb);
            }
        }

        /// <summary>
        /// Sends a private embed to a given user, alerting them in their current context if they're not already in a
        /// DM.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="user">The user to send the embed to.</param>
        /// <param name="eb">The embed to send.</param>
        /// <param name="notify">Whether or not to notify the user that they've been sent a message.</param>
        [NotNull]
        public async Task SendPrivateEmbedAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] IUser user,
            [NotNull] Embed eb,
            bool notify = true
        )
        {
            await user.SendMessageAsync(string.Empty, false, eb);

            if (!(context.Channel is IDMChannel) && notify)
            {
                await SendConfirmationAsync(context, "Please check your private messages.");
            }
        }

        /// <summary>
        /// Sends a private embed to a given user, alerting them in their current context if they're not already in a
        /// DM.
        /// </summary>
        /// <param name="context">The context of the command.</param>
        /// <param name="user">The user to send the embed to.</param>
        /// <param name="color">The color of the embed.</param>
        /// <param name="contents">The contents of the embed to send.</param>
        /// <param name="notify">Whether or not to notify the user that they've been sent a message.</param>
        [NotNull]
        public async Task SendPrivateEmbedAsync
        (
            [NotNull] ICommandContext context,
            [NotNull] IUser user,
            Color color,
            [NotNull] string contents,
            bool notify = true
        )
        {
            // Sometimes the content is > 2048 in length. We'll chunk it into embeds of 1024 here.
            if (contents.Length < 1024)
            {
                var eb = CreateEmbedBase(color);
                eb.WithDescription(contents);

                await user.SendMessageAsync(null, embed: eb.Build());
                return;
            }

            var words = contents.Split(' ');
            var messageBuilder = new StringBuilder();
            foreach (var word in words)
            {
                if (messageBuilder.Length >= 1024)
                {
                    var eb = CreateEmbedBase(color);
                    eb.WithDescription(messageBuilder.ToString());

                    await user.SendMessageAsync(null, embed: eb.Build());

                    messageBuilder.Clear();
                }

                messageBuilder.Append(word);
                messageBuilder.Append(" ");
            }

            if (messageBuilder.Length > 0)
            {
                var eb = CreateEmbedBase(color);
                eb.WithDescription(messageBuilder.ToString());

                await user.SendMessageAsync(null, embed: eb.Build());
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
        public Embed CreateFeedbackEmbed([NotNull] IMentionable invoker, Color color, [NotNull] string contents)
        {
            var eb = CreateEmbedBase(color);
            eb.WithDescription($"{invoker.Mention} | {contents}");

            return eb.Build();
        }

        /// <summary>
        /// Creates a base embed.
        /// </summary>
        /// <param name="color">The colour of the embed. Optional.</param>
        /// <returns>A basic embed.</returns>
        [Pure]
        [NotNull]
        public EmbedBuilder CreateEmbedBase(Color? color = null)
        {
            color ??= Color.DarkPurple;

            var eb = new EmbedBuilder();
            eb.WithColor(color.Value);

            return eb;
        }
    }
}
