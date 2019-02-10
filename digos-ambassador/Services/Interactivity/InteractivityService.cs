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

using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services.Interactivity.Messages;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services.Interactivity
{
    /// <summary>
    /// Acts as a Discord plugin for interactive messages.
    /// </summary>
    public class InteractivityService
    {
        private readonly IList<IInteractiveMessage> TrackedMessages;

        /// <summary>
        /// Gets the client that the service is attached to.
        /// </summary>
        public BaseSocketClient Client { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityService"/> class.
        /// </summary>
        /// <param name="client">The client to listen for messages from.</param>
        public InteractivityService(BaseSocketClient client)
        {
            this.Client = client;

            this.TrackedMessages = new List<IInteractiveMessage>();

            this.Client.ReactionAdded += OnReactionAdded;
            this.Client.ReactionRemoved += OnReactionRemoved;
            this.Client.MessageDeleted += OnMessageDeleted;
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
            [CanBeNull] Func<IUserMessage, bool> filter = null,
            [CanBeNull] TimeSpan? timeout = null
        )
        {
            filter = filter ?? (m => true);
            timeout = timeout ?? TimeSpan.FromSeconds(15);

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

            var task = await Task.WhenAny(trigger, delay).ConfigureAwait(false);

            this.Client.MessageReceived -= Handler;

            if (task == trigger)
            {
                return RetrieveEntityResult<IUserMessage>.FromSuccess(await trigger);
            }

            return RetrieveEntityResult<IUserMessage>.FromError
            (
                CommandError.ObjectNotFound,
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
        public Task SendInteractiveMessageAsync
        (
            [NotNull] IMessageChannel channel,
            [NotNull] IInteractiveMessage message
        )
        {
            this.TrackedMessages.Add(message);
            return message.SendAsync(this, channel);
        }

        /// <summary>
        /// Deletes an interactive message.
        /// </summary>
        /// <param name="message">The message to delete.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        public Task DeleteInteractiveMessageAsync([NotNull] IInteractiveMessage message)
        {
            this.TrackedMessages.Remove(message);
            return message.DeleteAsync();
        }

        /// <summary>
        /// Handles the deletion of a message, removing it from the tracking list.
        /// </summary>
        /// <param name="message">The deleted message.</param>
        /// <param name="channel">The channel the message was deleted in.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        private Task OnMessageDeleted(Cacheable<IMessage, ulong> message, [NotNull] ISocketMessageChannel channel)
        {
            var deletedMessages = this.TrackedMessages.Where(m => m.Message.Id == message.Id);
            foreach (var deletedMessage in deletedMessages)
            {
                this.TrackedMessages.Remove(deletedMessage);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles an removed reaction.
        /// </summary>
        /// <param name="message">The message the reaction was removed to.</param>
        /// <param name="channel">The channel the reaction was removed in.</param>
        /// <param name="reaction">The removed reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        private Task OnReactionRemoved
        (
            Cacheable<IUserMessage, ulong> message,
            [NotNull] ISocketMessageChannel channel,
            [NotNull] SocketReaction reaction
        )
        {
            if (reaction.User.IsSpecified && reaction.User.Value.IsMe(this.Client))
            {
                return Task.CompletedTask;
            }

            _ = Task.Run
            (
                async () =>
                {
                    var userMessage = await message.GetOrDownloadAsync();

                    var relevantMessages = this.TrackedMessages.Where(m => m.Message.Id == userMessage.Id);
                    var handlerTasks = relevantMessages
                        .Select(relevantMessage => relevantMessage.HandleRemovedInteractionAsync(reaction));

                    await Task.WhenAll(handlerTasks);
                }
            );

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handles an added reaction.
        /// </summary>
        /// <param name="message">The message the reaction was added to.</param>
        /// <param name="channel">The channel the reaction was added in.</param>
        /// <param name="reaction">The added reaction.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull]
        private Task OnReactionAdded
        (
            Cacheable<IUserMessage, ulong> message,
            [NotNull] ISocketMessageChannel channel,
            [NotNull] SocketReaction reaction
        )
        {
            if (reaction.User.IsSpecified && reaction.User.Value.IsMe(this.Client))
            {
                return Task.CompletedTask;
            }

            _ = Task.Run
            (
                async () =>
                {
                    var userMessage = await message.GetOrDownloadAsync();

                    var relevantMessages = this.TrackedMessages.Where(m => m.Message.Id == userMessage.Id);
                    var handlerTasks = relevantMessages
                        .Select(relevantMessage => relevantMessage.HandleAddedInteractionAsync(reaction)).ToList();

                    await Task.WhenAll(handlerTasks);
                }
            );

            return Task.CompletedTask;
        }
    }
}
