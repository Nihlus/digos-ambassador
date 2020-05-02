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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;

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
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        public EventLoggingBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<EventLoggingBehaviour> logger
        )
            : base(client, serviceScope, logger)
        {
        }

        /// <inheritdoc />
        protected override async Task UserLeft(SocketGuildUser user)
        {
            using var scope = this.Services.CreateScope();
            var loggingService = scope.ServiceProvider.GetRequiredService<ChannelLoggingService>();

            await loggingService.NotifyUserLeft(user);
        }

        /// <inheritdoc />
        protected override async Task GuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            using var scope = this.Services.CreateScope();
            var loggingService = scope.ServiceProvider.GetRequiredService<ChannelLoggingService>();

            if (oldMember.Username != newMember.Username)
            {
                await loggingService.NotifyUserUsernameChanged(newMember, oldMember.Username, newMember.Username);
            }

            if (oldMember.Discriminator != newMember.Discriminator)
            {
                await loggingService.NotifyUserDiscriminatorChanged
                (
                    newMember,
                    oldMember.Discriminator,
                    newMember.Discriminator
                );
            }
        }

        /// <inheritdoc />
        protected override async Task MessageDeleted(Cacheable<IMessage, ulong> message, ISocketMessageChannel channel)
        {
            if (!message.HasValue)
            {
                return;
            }

            using var scope = this.Services.CreateScope();
            var loggingService = scope.ServiceProvider.GetRequiredService<ChannelLoggingService>();

            await loggingService.NotifyMessageDeleted(message.Value, channel);
        }
    }
}
