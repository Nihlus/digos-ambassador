//
//  UserStatisticBehaviour.cs
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
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Remora.Discord.Behaviours;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Behaviours
{
    /// <summary>
    /// Acts on various user-related events, logging statistics..
    /// </summary>
    [UsedImplicitly]
    internal sealed class UserStatisticBehaviour : ClientEventBehaviour<UserStatisticBehaviour>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UserStatisticBehaviour"/> class.
        /// </summary>
        /// <param name="client">The discord client.</param>
        /// <param name="serviceScope">The service scope.</param>
        /// <param name="logger">The logging instance.</param>
        public UserStatisticBehaviour
        (
            DiscordSocketClient client,
            IServiceScope serviceScope,
            ILogger<UserStatisticBehaviour> logger
        )
            : base(client, serviceScope, logger)
        {
        }

        /// <inheritdoc/>
        protected override async Task MessageReceived(SocketMessage message)
        {
            if (!(message.Author is SocketGuildUser guildUser))
            {
                return;
            }

            if (!(message.Channel is SocketTextChannel textChannel))
            {
                return;
            }

            // First, let's get some valid service instances
            var autoroles = this.Services.GetRequiredService<AutoroleService>();

            var autorolesOnServer = autoroles.GetAutoroles(guildUser.Guild);

            var wantsToUpdateLocalStats =
                autorolesOnServer.Any(a => a.Conditions.Any(c => c is MessageCountInChannelCondition));

            var wantsToUpdateGlobalStats =
                autorolesOnServer.Any(a => a.Conditions.Any(c => c is MessageCountInGuildCondition));

            if (!(wantsToUpdateLocalStats || wantsToUpdateGlobalStats))
            {
                return;
            }

            var userStatistics = this.Services.GetRequiredService<UserStatisticsService>();

            if (wantsToUpdateLocalStats)
            {
                var getChannelStats = await userStatistics.GetOrCreateUserChannelStatisticsAsync
                (
                    guildUser,
                    textChannel
                );

                if (!getChannelStats.IsSuccess)
                {
                    this.Log.LogError(getChannelStats.Exception, getChannelStats.ErrorReason);
                    return;
                }

                var channelStats = getChannelStats.Entity;
                if (channelStats.MessageCount.HasValue)
                {
                    channelStats.MessageCount += 1;
                }
                else
                {
                    var countResult = await CountUserMessagesAsync(textChannel, guildUser);
                    if (countResult.IsSuccess)
                    {
                        channelStats.MessageCount = await countResult.Entity;
                    }
                }
            }

            if (wantsToUpdateGlobalStats)
            {
                var getGlobalStats = await userStatistics.GetOrCreateUserServerStatisticsAsync(guildUser);
                if (!getGlobalStats.IsSuccess)
                {
                    this.Log.LogError(getGlobalStats.Exception, getGlobalStats.ErrorReason);
                    return;
                }

                var globalStats = getGlobalStats.Entity;

                if (globalStats.TotalMessageCount.HasValue)
                {
                    globalStats.TotalMessageCount += 1;
                }
                else
                {
                    // Compute the first-time sum
                    long sum = 0;
                    foreach (var guildChannel in guildUser.Guild.TextChannels)
                    {
                        var countResult = await CountUserMessagesAsync(guildChannel, guildUser);
                        if (!countResult.IsSuccess)
                        {
                            continue;
                        }

                        sum += await countResult.Entity;
                    }

                    globalStats.TotalMessageCount = sum;
                }
            }

            await userStatistics.SaveChangesAsync();
        }

        private async Task<RetrieveEntityResult<Task<long>>> CountUserMessagesAsync(IMessageChannel channel, IUser user)
        {
            long sum = 0;
            try
            {
                var latestMessage = (await channel.GetMessagesAsync(1).FlattenAsync()).FirstOrDefault();
                if (latestMessage is null)
                {
                    return RetrieveEntityResult<Task<long>>.FromError("No messages in channel.");
                }

                while (true)
                {
                    var channelMessages = channel.GetMessagesAsync(latestMessage, Direction.Before);

                    var processedBatches = 0;
                    await foreach (var channelMessageBatch in channelMessages)
                    {
                        if (channelMessageBatch.Count == 0)
                        {
                            continue;
                        }

                        foreach (var channelMessage in channelMessageBatch)
                        {
                            latestMessage = channelMessage;
                            if (channelMessage.Author.Id == user.Id)
                            {
                                sum += 1;
                            }
                        }

                        processedBatches += 1;
                    }

                    if (processedBatches == 0)
                    {
                        break;
                    }
                }
            }
            catch (HttpException hex) when (hex.WasCausedByMissingPermission())
            {
                return RetrieveEntityResult<Task<long>>.FromError("No permissions to read the channel.");
            }
            catch (Exception ex)
            {
                return RetrieveEntityResult<Task<long>>.FromError(ex);
            }

            return Task.FromResult(sum);
        }
    }
}
