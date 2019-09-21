//
//  RoleplayLoggingBehaviour.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Behaviours;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Behaviours
{
    /// <summary>
    /// Acts on user messages, logging them into an active roleplay if relevant.
    /// </summary>
    [UsedImplicitly]
    internal sealed class RoleplayLoggingBehaviour : ClientEventBehaviour
    {
        [NotNull]
        private readonly RoleplayService _roleplays;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayLoggingBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="roleplays">The roleplay service.</param>
        public RoleplayLoggingBehaviour
        (
            [NotNull] DiscordSocketClient client,
            [NotNull] RoleplayService roleplays
        )
            : base(client)
        {
            _roleplays = roleplays;
        }

        /// <summary>
        /// Ensures active roleplays are rescanned on startup in order to catch missed messages.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task Connected()
        {
            var activeRoleplays = _roleplays.GetRoleplays()
                .Where(r => r.DedicatedChannelID.HasValue);

            foreach (var activeRoleplay in activeRoleplays)
            {
                var guild = this.Client.GetGuild((ulong)activeRoleplay.ServerID);

                // ReSharper disable once PossibleInvalidOperationException
                var channel = guild.GetTextChannel((ulong)activeRoleplay.DedicatedChannelID.Value);

                foreach (var message in await channel.GetMessagesAsync().FlattenAsync())
                {
                    // We don't care about the results here.
                    await _roleplays.AddToOrUpdateMessageInRoleplayAsync(activeRoleplay, message);
                }
            }
        }

        /// <inheritdoc />
        protected override async Task MessageReceived(SocketMessage arg)
        {
            if (!(arg is SocketUserMessage message))
            {
                return;
            }

            if (arg.Author.IsBot || arg.Author.IsWebhook)
            {
                return;
            }

            var discard = 0;

            if (message.HasCharPrefix('!', ref discard))
            {
                return;
            }

            if (message.HasMentionPrefix(this.Client.CurrentUser, ref discard))
            {
                return;
            }

            await _roleplays.ConsumeMessageAsync(new SocketCommandContext(this.Client, message));
        }

        /// <inheritdoc />
        protected override async Task MessageUpdated
        (
            Cacheable<IMessage, ulong> oldMessage,
            [CanBeNull] SocketMessage updatedMessage,
            ISocketMessageChannel messageChannel
        )
        {
            if (updatedMessage is null)
            {
                return;
            }

            // Ignore all changes except text changes
            var isTextUpdate = updatedMessage.EditedTimestamp.HasValue &&
                                updatedMessage.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes();

            if (!isTextUpdate)
            {
                return;
            }

            await MessageReceived(updatedMessage);
        }
    }
}
