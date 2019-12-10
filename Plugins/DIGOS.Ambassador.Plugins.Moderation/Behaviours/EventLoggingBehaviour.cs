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
using DIGOS.Ambassador.Discord.Behaviours;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using Discord;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DIGOS.Ambassador.Plugins.Moderation.Behaviours
{
    /// <summary>
    /// Logs various client events.
    /// </summary>
    [UsedImplicitly]
    public class EventLoggingBehaviour : ClientEventBehaviour<EventLoggingBehaviour>
    {
        private readonly ChannelLoggingService _logging;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventLoggingBehaviour"/> class.
        /// </summary>
        /// <param name="client">The Discord client in use.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        /// <param name="logging">The logging service.</param>
        public EventLoggingBehaviour
        (
            [NotNull] DiscordSocketClient client,
            [NotNull] IServiceScope serviceScope,
            [NotNull] ILogger<EventLoggingBehaviour> logger,
            [NotNull] ChannelLoggingService logging
        )
            : base(client, serviceScope, logger)
        {
            _logging = logging;
        }

        /// <inheritdoc />
        protected override async Task UserLeft(SocketGuildUser user)
        {
            await _logging.NotifyUserLeft(user);
        }

        /// <inheritdoc />
        protected override async Task GuildMemberUpdated(SocketGuildUser oldMember, SocketGuildUser newMember)
        {
            if (oldMember.Username != newMember.Username)
            {
                await _logging.NotifyUserUsernameChanged(newMember, oldMember.Username, newMember.Username);
            }

            if (oldMember.Discriminator != newMember.Discriminator)
            {
                await _logging.NotifyUserDiscriminatorChanged
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

            await _logging.NotifyMessageDeleted(message.Value, channel);
        }
    }
}
