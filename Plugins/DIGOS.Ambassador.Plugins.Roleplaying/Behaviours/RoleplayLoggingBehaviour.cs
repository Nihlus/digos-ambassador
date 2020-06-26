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
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Behaviours
{
    /// <summary>
    /// Acts on user messages, logging them into an active roleplay if relevant.
    /// </summary>
    [UsedImplicitly]
    internal sealed class RoleplayLoggingBehaviour : ClientEventBehaviour<RoleplayLoggingBehaviour>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayLoggingBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="serviceScope">The service scope in use.</param>
        /// <param name="logger">The logging instance for this type.</param>
        public RoleplayLoggingBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<RoleplayLoggingBehaviour> logger
        )
            : base(client, serviceScope, logger)
        {
        }

        /// <summary>
        /// Ensures active roleplays are rescanned on startup in order to catch missed messages.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task Connected()
        {
            using var connectionScope = this.Services.CreateScope();
            var roleplayService = connectionScope.ServiceProvider.GetRequiredService<RoleplayDiscordService>();

            foreach (var guild in this.Client.Guilds)
            {
                var getRoleplays = await roleplayService.GetRoleplaysAsync(guild);
                if (!getRoleplays.IsSuccess)
                {
                    continue;
                }

                var guildRoleplays = getRoleplays.Entity;

                var activeRoleplays = guildRoleplays.Where(r => r.DedicatedChannelID.HasValue).ToList();
                foreach (var activeRoleplay in activeRoleplays)
                {
                    var channel = guild.GetTextChannel((ulong)activeRoleplay.DedicatedChannelID!.Value);
                    if (channel is null)
                    {
                        continue;
                    }

                    var updatedMessages = 0;
                    foreach (var message in await channel.GetMessagesAsync().FlattenAsync())
                    {
                        if (!(message is IUserMessage userMessage))
                        {
                            continue;
                        }

                        // We don't care about the results here.
                        var updateResult = await roleplayService.ConsumeMessageAsync(userMessage);
                        if (updateResult.IsSuccess)
                        {
                            ++updatedMessages;
                        }
                    }

                    if (updatedMessages > 0)
                    {
                        this.Log.LogInformation
                        (
                            $"Added or updated {updatedMessages} missed {(updatedMessages > 1 ? "messages" : "message")} " +
                            $"in \"{activeRoleplay.Name}\"."
                        );
                    }
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

            using var messageScope = this.Services.CreateScope();
            var roleplayService = messageScope.ServiceProvider.GetRequiredService<RoleplayDiscordService>();

            await roleplayService.ConsumeMessageAsync(message);
        }

        /// <inheritdoc />
        protected override async Task MessageUpdated
        (
            Cacheable<IMessage, ulong> oldMessage,
            SocketMessage updatedMessage,
            ISocketMessageChannel messageChannel
        )
        {
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
