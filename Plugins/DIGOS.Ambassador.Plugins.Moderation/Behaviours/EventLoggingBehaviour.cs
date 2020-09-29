//
//  EventLoggingBehaviour.cs
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
using DIGOS.Ambassador.Plugins.Moderation.Services;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Behaviours
{
    /// <summary>
    /// Logs various client events.
    /// </summary>
    [UsedImplicitly]
    public class EventLoggingBehaviour : ClientEventBehaviour<EventLoggingBehaviour>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventLoggingBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client in use.</param>
        /// <param name="services">The services.</param>
        /// <param name="logger">The logging instance for this type.</param>
        public EventLoggingBehaviour
        (
            DiscordSocketClient client,
            IServiceProvider services,
            ILogger<EventLoggingBehaviour> logger
        )
            : base(client, services, logger)
        {
        }

        /// <inheritdoc />
        protected override async Task<OperationResult> UserLeftAsync(SocketGuildUser user)
        {
            using var scope = this.Services.CreateScope();
            var loggingService = scope.ServiceProvider.GetRequiredService<ChannelLoggingService>();

            return await loggingService.NotifyUserLeftAsync(user);
        }

        /// <inheritdoc />
        protected override async Task<OperationResult> GuildMemberUpdatedAsync
        (
            SocketGuildUser oldMember,
            SocketGuildUser newMember
        )
        {
            using var scope = this.Services.CreateScope();
            var loggingService = scope.ServiceProvider.GetRequiredService<ChannelLoggingService>();

            if (oldMember.Username != newMember.Username)
            {
                var notifyResult = await loggingService.NotifyUserUsernameChangedAsync
                (
                    newMember,
                    oldMember.Username,
                    newMember.Username
                );

                if (!notifyResult.IsSuccess)
                {
                    return OperationResult.FromError(notifyResult);
                }
            }

            if (oldMember.Discriminator != newMember.Discriminator)
            {
                return await loggingService.NotifyUserDiscriminatorChangedAsync
                (
                    newMember,
                    oldMember.Discriminator,
                    newMember.Discriminator
                );
            }

            return OperationResult.FromSuccess();
        }

        /// <inheritdoc />
        protected override async Task<OperationResult> MessageDeletedAsync
        (
            Cacheable<IMessage, ulong> message,
            ISocketMessageChannel channel
        )
        {
            if (!message.HasValue)
            {
                return OperationResult.FromSuccess();
            }

            using var scope = this.Services.CreateScope();
            var loggingService = scope.ServiceProvider.GetRequiredService<ChannelLoggingService>();

            return await loggingService.NotifyMessageDeletedAsync(message.Value, channel);
        }
    }
}
