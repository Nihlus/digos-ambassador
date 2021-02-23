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
using System.Collections.Concurrent;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Interactivity
{
    /// <summary>
    /// Acts as a Discord plugin for interactive messages.
    /// </summary>
    public class InteractivityService
    {
        /// <summary>
        /// Holds a mapping of message IDs to tracked messages.
        /// </summary>
        private readonly ConcurrentDictionary<Snowflake, IInteractiveMessage> _trackedMessages = new();

        /// <summary>
        /// Holds a mapping of tracked messages to synchronization primitives.
        /// </summary>
        private readonly ConcurrentDictionary<IInteractiveMessage, SemaphoreSlim> _messageSemaphores = new();

        /// <summary>
        /// Holds the Discord channel API.
        /// </summary>
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityService"/> class.
        /// </summary>
        /// <param name="channelAPI">The channel API.</param>
        public InteractivityService(IDiscordRestChannelAPI channelAPI)
        {
            _channelAPI = channelAPI;
        }

        /// <summary>
        /// Sends an interactive message.
        /// </summary>
        /// <param name="channelID">The channel to send the message in.</param>
        /// <param name="messageFactory">A factory function that wraps a sent message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<Result> SendInteractiveMessageAsync
        (
            Snowflake channelID,
            Func<Snowflake, Snowflake, IDiscordRestChannelAPI, IInteractiveMessage> messageFactory,
            CancellationToken ct = default
        )
        {
            var initialEmbed = new Embed
            {
                Colour = Color.Gray,
                Description = "Loading..."
            };

            var sendMessage = await _channelAPI.CreateMessageAsync(channelID, embed: initialEmbed, ct: ct);
            if (!sendMessage.IsSuccess)
            {
                return Result.FromError(sendMessage);
            }

            var message = sendMessage.Entity;
            var interactiveMessage = messageFactory(channelID, message.ID, _channelAPI);

            var updateMessage = await interactiveMessage.UpdateAsync(ct);
            if (!updateMessage.IsSuccess)
            {
                return updateMessage;
            }

            return TrackMessage(message.ID, interactiveMessage);
        }

        /// <summary>
        /// Begins tracking the given message.
        /// </summary>
        /// <param name="id">The ID of the sent message.</param>
        /// <param name="message">The interactive message.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public Result TrackMessage(Snowflake id, IInteractiveMessage message)
        {
            if (!_trackedMessages.TryAdd(id, message))
            {
                return new GenericError("A message with that ID is already tracked.");
            }

            if (!_messageSemaphores.TryAdd(message, new SemaphoreSlim(1, 1)))
            {
                return new GenericError("A semaphore is already registered for that message.");
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Ceases tracking the message with the given ID.
        /// </summary>
        /// <param name="id">The ID of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<Result> UntrackMessageAsync(Snowflake id, CancellationToken ct = default)
        {
            if (!_trackedMessages.TryRemove(id, out var trackedMessage))
            {
                // The message is already removed
                return Result.FromSuccess();
            }

            if (!_messageSemaphores.TryRemove(trackedMessage, out var semaphore))
            {
                // The semaphore is already removed
                return Result.FromSuccess();
            }

            await semaphore.WaitAsync(ct);
            semaphore.Release();
            semaphore.Dispose();

            return Result.FromSuccess();
        }

        /// <summary>
        /// Dispatches an added reaction to interested messages.
        /// </summary>
        /// <param name="userID">The ID of the user who added the reaction.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="emoji">The emoji.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<Result> OnReactionAddedAsync
        (
            Snowflake userID,
            Snowflake messageID,
            IPartialEmoji emoji,
            CancellationToken ct = default
        )
        {
            if (!_trackedMessages.TryGetValue(messageID, out var message))
            {
                return Result.FromSuccess();
            }

            if (!_messageSemaphores.TryGetValue(message, out var semaphore))
            {
                throw new InvalidOperationException("Failed to get a semaphore for a tracked message.");
            }

            await semaphore.WaitAsync(ct);
            try
            {
                var result = await message.OnReactionAddedAsync(userID, emoji, ct);
                if (!result.IsSuccess)
                {
                    return result;
                }
            }
            finally
            {
                semaphore.Release();
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Dispatches a removed reaction to interested messages.
        /// </summary>
        /// <param name="userID">The ID of the user who removed the reaction.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="emoji">The emoji.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<Result> OnReactionRemovedAsync
        (
            Snowflake userID,
            Snowflake messageID,
            IPartialEmoji emoji,
            CancellationToken ct = default
        )
        {
            if (!_trackedMessages.TryGetValue(messageID, out var message))
            {
                return Result.FromSuccess();
            }

            if (!_messageSemaphores.TryGetValue(message, out var semaphore))
            {
                throw new InvalidOperationException("Failed to get a semaphore for a tracked message.");
            }

            await semaphore.WaitAsync(ct);
            try
            {
                var result = await message.OnReactionRemovedAsync(userID, emoji, ct);
                if (!result.IsSuccess)
                {
                    return result;
                }
            }
            finally
            {
                semaphore.Release();
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Dispatches a complete removal of all reactions to interested messages.
        /// </summary>
        /// <param name="id">The ID of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<Result> OnAllReactionsRemovedAsync(Snowflake id, CancellationToken ct = default)
        {
            if (!_trackedMessages.TryGetValue(id, out var message))
            {
                return Result.FromSuccess();
            }

            if (!_messageSemaphores.TryGetValue(message, out var semaphore))
            {
                throw new InvalidOperationException("Failed to get a semaphore for a tracked message.");
            }

            await semaphore.WaitAsync(ct);
            try
            {
                var result = await message.OnAllReactionsRemovedAsync(ct);
                if (!result.IsSuccess)
                {
                    return result;
                }
            }
            finally
            {
                semaphore.Release();
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Ceases tracking the message with the given ID.
        /// </summary>
        /// <param name="id">The ID of the message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public Task<Result> OnMessageDeletedAsync(Snowflake id, CancellationToken ct = default)
            => UntrackMessageAsync(id, ct);
    }
}
