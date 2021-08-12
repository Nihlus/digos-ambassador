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
using Remora.Discord.Commands.Feedback.Services;
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
        /// Holds the Discord channel API.
        /// </summary>
        private readonly IDiscordRestChannelAPI _channelAPI;

        /// <summary>
        /// Holds the available services.
        /// </summary>
        private readonly IServiceProvider _services;

        /// <summary>
        /// Holds the feedback service.
        /// </summary>
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Gets the message tracker.
        /// </summary>
        public InteractiveMessageTracker Tracker { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="InteractivityService"/> class.
        /// </summary>
        /// <param name="services">The available services.</param>
        /// <param name="tracker">The message tracker.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="feedback">The feedback service.</param>
        public InteractivityService
        (
            IServiceProvider services,
            InteractiveMessageTracker tracker,
            IDiscordRestChannelAPI channelAPI,
            FeedbackService feedback
        )
        {
            _channelAPI = channelAPI;
            _feedback = feedback;
            _services = services;

            this.Tracker = tracker;
        }

        /// <summary>
        /// Sends an interactive message to the current context.
        /// </summary>
        /// <param name="messageFactory">A factory function that wraps a sent message.</param>
        /// <param name="ct">The cancellation token for this operation.</param>
        /// <returns>A result which may or may not have succeeded.</returns>
        public async Task<Result> SendContextualInteractiveMessageAsync
        (
            Func<Snowflake, Snowflake, IInteractiveMessage> messageFactory,
            CancellationToken ct = default
        )
        {
            var initialEmbed = new Embed
            {
                Colour = Color.Gray,
                Description = "Loading..."
            };

            var sendMessage = await _feedback.SendContextualEmbedAsync(initialEmbed, ct);
            if (!sendMessage.IsSuccess)
            {
                return Result.FromError(sendMessage);
            }

            var message = sendMessage.Entity;
            var interactiveMessage = messageFactory(message.ChannelID, message.ID);
            var trackMessage = this.Tracker.TrackMessage(interactiveMessage);
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
    }
}
