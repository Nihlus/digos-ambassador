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

using System;
using System.Threading.Tasks;

using DIGOS.Ambassador.Interactivity;
using DIGOS.Ambassador.Pagination;
using DIGOS.Ambassador.Services;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.Net;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Extensions
{
    /// <summary>
    /// Contains extensions to the <see cref="InteractiveService"/> class.
    /// </summary>
    [PublicAPI]
    public static class InteractiveServiceExtensions
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(15);

        /// <summary>
        /// Sends a message and deletes it after a specified timeout.
        /// </summary>
        /// <param name="this">The interactive service.</param>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="content">The content of the message.</param>
        /// <param name="isTTS">Whether or not the message is a TTS message.</param>
        /// <param name="embed">The embed to have in the message. Optional.</param>
        /// <param name="timeout">The timeout before the message is deleted. Defaults to 15 seconds.</param>
        /// <param name="options">Any options that should be passed along with the request.</param>
        /// <returns>A user message.</returns>
        [PublicAPI]
        public static async Task<IUserMessage> ReplyAndDeleteAsync
        (
            this InteractiveService @this,
            [NotNull] IMessageChannel channel,
            [NotNull] string content,
            bool isTTS = false,
            [CanBeNull] Embed embed = null,
            TimeSpan? timeout = null,
            [CanBeNull] RequestOptions options = null
        )
        {
            timeout = timeout ?? DefaultTimeout;

            var message = await channel.SendMessageAsync(content, isTTS, embed, options).ConfigureAwait(false);

            // ReSharper disable once AssignmentIsFullyDiscarded
            _ = Task.Delay(timeout.Value)
                .ContinueWith(c => message.DeleteAsync().ConfigureAwait(false))
                .ConfigureAwait(false);

            return message;
        }

        /// <summary>
        /// Sends an interactive message to the context user's direct messaging channel, alerting them if they are
        /// not already in it.
        /// </summary>
        /// <param name="this">The interactive service.</param>
        /// <param name="context">The message context.</param>
        /// <param name="feedback">The user feedback service.</param>
        /// <param name="interactiveMessage">The interactive message.</param>
        /// <returns>The underlying sent message.</returns>
        [PublicAPI]
        public static async Task<IUserMessage> SendPrivateInteractiveMessageAsync
        (
            this InteractiveService @this,
            [NotNull] SocketCommandContext context,
            [NotNull] UserFeedbackService feedback,
            [NotNull] IInteractiveMessage interactiveMessage
        )
        {
            if (!(await context.User.GetOrCreateDMChannelAsync() is ISocketMessageChannel userChannel))
            {
                throw new InvalidOperationException("Could not create DM channel for target user.");
            }

            try
            {
                await feedback.SendConfirmationAsync(context, "Loading...");
            }
            catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
            {
                await feedback.SendWarningAsync(context, "You don't accept DMs from non-friends on this server, so I'm unable to do that.");
                throw new InvalidOperationException("User does not accept DMs from non-friends.");
            }
            finally
            {
                await ((IDMChannel)userChannel).CloseAsync();
            }

            if (!context.IsPrivate)
            {
                await feedback.SendConfirmationAsync(context, "Please check your private messages.");
            }

            return await SendInteractiveMessageAsync(@this, context, interactiveMessage, userChannel);
        }

        /// <summary>
        /// Sends an interactive message to the given channel.
        /// </summary>
        /// <param name="this">The interactive service.</param>
        /// <param name="context">The message context.</param>
        /// <param name="interactiveMessage">The interactive message.</param>
        /// <param name="channel">The channel to send the message to. Defaults to the context channel.</param>
        /// <returns>The underlying sent message.</returns>
        [PublicAPI]
        public static async Task<IUserMessage> SendInteractiveMessageAsync
        (
            this InteractiveService @this,
            [NotNull] SocketCommandContext context,
            [NotNull] IInteractiveMessage interactiveMessage,
            [CanBeNull] ISocketMessageChannel channel = null
        )
        {
            channel = channel ?? context.Channel;

            var message = await interactiveMessage.DisplayAsync(channel);

            if (interactiveMessage.ReactionCallback is null)
            {
                return message;
            }

            @this.AddReactionCallback(message, interactiveMessage.ReactionCallback);

            if (interactiveMessage.Timeout.HasValue)
            {
                // ReSharper disable once AssignmentIsFullyDiscarded
                _ = Task.Delay(interactiveMessage.Timeout.Value).ContinueWith(c =>
                {
                    @this.RemoveReactionCallback(interactiveMessage.Message);
                    interactiveMessage.Message.DeleteAsync();
                });
            }

            return message;
        }

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
        [PublicAPI]
        public static async Task<IUserMessage> SendPrivatePaginatedMessageAsync<T1, T2>
        (
            this InteractiveService @this,
            [NotNull] SocketCommandContext context,
            [NotNull] UserFeedbackService feedback,
            [NotNull] IPager<T1, T2> pager,
            [CanBeNull] ICriterion<SocketReaction> criterion = null
        )
            where T2 : IPager<T1, T2>
        {
            var userChannel = await context.User.GetOrCreateDMChannelAsync();
            try
            {
                await feedback.SendConfirmationAsync(context, "Loading...");
            }
            catch (HttpException hex)
            {
                if (hex.WasCausedByDMsNotAccepted())
                {
                    await feedback.SendWarningAsync(context, "You don't accept DMs from non-friends on this server, so I'm unable to do that.");
                    throw new InvalidOperationException("User does not accept DMs from non-friends.");
                }
            }
            finally
            {
                await userChannel.CloseAsync();
            }

            if (!context.IsPrivate)
            {
                await feedback.SendConfirmationAsync(context, "Please check your private messages.");
            }

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
        [PublicAPI]
        public static async Task<IUserMessage> SendPaginatedMessageAsync<T1, T2>
        (
            this InteractiveService @this,
            [NotNull] SocketCommandContext context,
            [NotNull] UserFeedbackService feedback,
            [NotNull] IPager<T1, T2> pager,
            [CanBeNull] IMessageChannel channel = null,
            [CanBeNull] ICriterion<SocketReaction> criterion = null
        )
        where T2 : IPager<T1, T2>
        {
            var callback = new PaginatedCallback<T1, T2>(@this, feedback, context, pager, channel, criterion);
            await callback.DisplayAsync().ConfigureAwait(false);

            return callback.Message;
        }
    }
}
