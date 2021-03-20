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
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using DIGOS.Ambassador.Discord.Interactivity.Responders;
using Microsoft.Extensions.DependencyInjection;
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
        private readonly ConcurrentDictionary<string, IInteractiveMessage> _trackedMessages = new();

        /// <summary>
        /// Holds the Discord channel API.
        /// </summary>
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Holds the Discord user API.
        /// </summary>
        private readonly IDiscordRestUserAPI _userAPI;

        /// <summary>
        /// Holds the available services.
        /// </summary>
        private readonly IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityService"/> class.
        /// </summary>
        /// <param name="services">The available services.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="userAPI">The user API.</param>
        public InteractivityService
        (
            IServiceProvider services,
            IDiscordRestChannelAPI channelAPI,
            IDiscordRestUserAPI userAPI
        )
        {
            _channelAPI = channelAPI;
            _userAPI = userAPI;
            _services = services;
        }

        /// <summary>
        /// Sends an interactive message.
        /// </summary>
        /// <param name="userID">The user to send the message to.</param>
        /// <param name="messageFactory">A factory function that wraps a sent message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<Result> SendPrivateInteractiveMessageAsync
        (
            Snowflake userID,
            Func<Snowflake, Snowflake, IInteractiveMessage> messageFactory,
            CancellationToken ct = default
        )
        {
            var createDM = await _userAPI.CreateDMAsync(userID, ct);
            if (!createDM.IsSuccess)
            {
                return Result.FromError(createDM);
            }

            var dm = createDM.Entity;

            return await SendInteractiveMessageAsync(dm.ID, messageFactory, ct);
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
            Func<Snowflake, Snowflake, IInteractiveMessage> messageFactory,
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
            var interactiveMessage = messageFactory(channelID, message.ID);
            var trackMessage = TrackMessage(interactiveMessage);
            if (!trackMessage.IsSuccess)
            {
                return trackMessage;
            }

            var interestedResponders = _services.GetServices<InteractivityResponder>();
            foreach (var responder in interestedResponders)
            {
                var updateMessage = await responder.OnCreateAsync(interactiveMessage.Nonce, ct);
                if (!updateMessage.IsSuccess)
                {
                    return updateMessage;
                }
            }

            return Result.FromSuccess();
        }

        /// <summary>
        /// Begins tracking the given message.
        /// </summary>
        /// <param name="message">The interactive message.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public Result TrackMessage(IInteractiveMessage message)
        {
            if (!_trackedMessages.TryAdd(message.Nonce, message))
            {
                return new GenericError("A message with that ID is already tracked.");
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
            if (!_trackedMessages.TryRemove(id.ToString(), out var trackedMessage))
            {
                // The message is already removed
                return Result.FromSuccess();
            }

            await trackedMessage.Semaphore.WaitAsync(ct);
            trackedMessage.Dispose();

            return Result.FromSuccess();
        }

        /// <summary>
        /// Gets a registered interactive entity that matches the given nonce.
        /// </summary>
        /// <param name="nonce">The entity's unique identifier.</param>
        /// <param name="entity">The entity, or null if none exists.</param>
        /// <typeparam name="TEntity">The concrete entity type.</typeparam>
        /// <returns>true if a matching entity was successfully found; otherwise, false.</returns>
        public bool TryGetInteractiveEntity<TEntity>(string nonce, [NotNullWhen(true)] out TEntity? entity)
            where TEntity : IInteractiveEntity
        {
            entity = default;

            if (!_trackedMessages.TryGetValue(nonce, out var untypedEntity))
            {
                return false;
            }

            if (untypedEntity is not TEntity typedEntity)
            {
                return false;
            }

            entity = typedEntity;
            return true;
        }

        /// <summary>
        /// Gets the next message sent in the given channel.
        /// </summary>
        /// <param name="channelID">The channel to watch.</param>
        /// <param name="timeout">The timeout, after which the method gives up.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>The message, or null if no message was sent within the given timespan.</returns>
        public async Task<Result<IMessage?>> GetNextMessageAsync
        (
            Snowflake channelID,
            TimeSpan timeout,
            CancellationToken ct = default
        )
        {
            var now = DateTimeOffset.UtcNow;
            var timeoutTime = now + timeout;
            var after = Snowflake.CreateTimestampSnowflake(now);

            while (now <= timeoutTime)
            {
                now = DateTimeOffset.UtcNow;
                var getMessage = await _channelAPI.GetChannelMessagesAsync
                (
                    channelID,
                    after: after,
                    limit: 1,
                    ct: ct
                );

                if (!getMessage.IsSuccess)
                {
                    return Result<IMessage?>.FromError(getMessage);
                }

                var message = getMessage.Entity.FirstOrDefault();
                if (message is not null)
                {
                    return Result<IMessage?>.FromSuccess(message);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(500), ct);
            }

            return Result<IMessage?>.FromSuccess(null);
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
