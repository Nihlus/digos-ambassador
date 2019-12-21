//
//  InteractivityService.cs
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
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Interactivity
{
    /// <summary>
    /// Acts as a Discord plugin for interactive messages.
    /// </summary>
    public class InteractivityService
    {
        private readonly DelayedActionService _delayedActions;
        private readonly IList<IInteractiveMessage> _trackedMessages;

        /// <summary>
        /// Gets the client that the service is attached to.
        /// </summary>
        public BaseSocketClient Client { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityService"/> class.
        /// </summary>
        /// <param name="client">The client to listen for messages from.</param>
        /// <param name="delayedActions">The delayed actions service.</param>
        public InteractivityService(BaseSocketClient client, DelayedActionService delayedActions)
        {
            this.Client = client;
            _delayedActions = delayedActions;

            _trackedMessages = new List<IInteractiveMessage>();
        }

        /// <summary>
        /// Gets the next message in the given channel.
        /// </summary>
        /// <param name="channel">The channel to watch for the next message.</param>
        /// <param name="filter">A filter function that determines if the message should be accepted.</param>
        /// <param name="timeout">A timeout after which the function will return. Defaults to 15 seconds.</param>
        /// <returns>A result that may contain the message.</returns>
        public async Task<RetrieveEntityResult<IUserMessage>> GetNextMessageAsync
        (
            [NotNull] IMessageChannel channel,
            Func<IUserMessage, bool>? filter = null,
            TimeSpan? timeout = null
        )
        {
            filter ??= m => true;
            timeout ??= TimeSpan.FromSeconds(15);

            var messageTrigger = new TaskCompletionSource<IUserMessage>();

            Task Handler(SocketMessage message)
            {
                if (!(message is IUserMessage userMessage))
                {
                    return Task.CompletedTask;
                }

                if (userMessage.Channel.Id != channel.Id)
                {
                    return Task.CompletedTask;
                }

                if (filter(userMessage))
                {
                    messageTrigger.SetResult(userMessage);
                }

                return Task.CompletedTask;
            }

            this.Client.MessageReceived += Handler;

            var trigger = messageTrigger.Task;
            var delay = Task.Delay(timeout.Value);

            var task = await Task.WhenAny(trigger, delay);

            this.Client.MessageReceived -= Handler;

            if (task == trigger)
            {
                return RetrieveEntityResult<IUserMessage>.FromSuccess(await trigger);
            }

            return RetrieveEntityResult<IUserMessage>.FromError
            (
                "No accepted message received within the timeout period."
            );
        }

        /// <summary>
        /// Sends an interactive message to the given channel.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="message">The message to send.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        public async Task SendInteractiveMessageAsync
        (
            [NotNull] IMessageChannel channel,
            [NotNull] IInteractiveMessage message
        )
        {
            _trackedMessages.Add(message);
            await message.SendAsync(this, channel);

            while (_trackedMessages.Contains(message))
            {
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }

        /// <summary>
        /// Sends an interactive message to the given channel and deletes it after a certain timeout.
        /// </summary>
        /// <param name="channel">The channel to send the message to.</param>
        /// <param name="message">The message to send.</param>
        /// <param name="timeout">The timeout after which the embed will be deleted. Defaults to 15 seconds.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendInteractiveMessageAndDeleteAsync
        (
            [NotNull] IMessageChannel channel,
            [NotNull] IInteractiveMessage message,
            TimeSpan? timeout = null
        )
        {
            timeout ??= TimeSpan.FromSeconds(15.0);

            _trackedMessages.Add(message);
            await message.SendAsync(this, channel);

            _delayedActions.DelayUntil(() => DeleteInteractiveMessageAsync(message), timeout.Value);
        }

        /// <summary>
        /// Deletes an interactive message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        public Task DeleteInteractiveMessageAsync([NotNull] IInteractiveMessage message)
        {
            _trackedMessages.Remove(message);
            return message.DeleteAsync();
        }

        /// <summary>
        /// Handles the deletion of a message, removing it from the tracking list.
        /// </summary>
        /// <param name="message">The deleted message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        internal async Task OnMessageDeleted
        (
            Cacheable<IMessage, ulong> message
        )
        {
            var userMessage = await message.GetOrDownloadAsync();
            if (userMessage is null)
            {
                return;
            }

            var deletedMessages = _trackedMessages.Where(m => m.Message?.Id == userMessage.Id);
            foreach (var deletedMessage in deletedMessages)
            {
                _trackedMessages.Remove(deletedMessage);
            }
        }

        /// <summary>
        /// Handles reception of a message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal Task OnMessageReceived(SocketMessage message)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles an removed reaction.
        /// </summary>
        /// <param name="message">The message the reaction was removed to.</param>
        /// <param name="reaction">The removed reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        internal async Task OnReactionRemoved
        (
            Cacheable<IUserMessage, ulong> message,
            [NotNull] SocketReaction reaction
        )
        {
            if (reaction.User.IsSpecified && reaction.User.Value.IsMe(this.Client))
            {
                return;
            }

            var userMessage = await message.GetOrDownloadAsync();
            if (userMessage is null)
            {
                return;
            }

            var relevantMessages = _trackedMessages.Where(m => m.Message?.Id == userMessage.Id);
            var handlerTasks = relevantMessages
                .Select(relevantMessage => relevantMessage.HandleRemovedInteractionAsync(reaction));

            await Task.WhenAll(handlerTasks);
        }

        /// <summary>
        /// Handles an added reaction.
        /// </summary>
        /// <param name="message">The message the reaction was added to.</param>
        /// <param name="reaction">The added reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        internal async Task OnReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            [NotNull] SocketReaction reaction
        )
        {
            if (reaction.User.IsSpecified && reaction.User.Value.IsMe(this.Client))
            {
                return;
            }

            var userMessage = await message.GetOrDownloadAsync();
            if (userMessage is null)
            {
                return;
            }

            var relevantMessages = _trackedMessages.Where(m => m.Message?.Id == userMessage.Id);
            var handlerTasks = relevantMessages
                .Select(relevantMessage => relevantMessage.HandleAddedInteractionAsync(reaction)).ToList();

            await Task.WhenAll(handlerTasks);
        }
    }
}
