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
    /// Acts on various user-related events, logging statistics.
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
        protected override async Task<OperationResult> MessageUpdatedAsync
        (
            Cacheable<IMessage, ulong> oldMessage,
            SocketMessage newMessage,
            ISocketMessageChannel channel
        )
        {
            if (newMessage.Author.IsBot || newMessage.Author.IsWebhook)
            {
                return OperationResult.FromSuccess();
            }

            if (!(newMessage.Author is IGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            using var eventScope = this.Services.CreateScope();
            var userStatistics = eventScope.ServiceProvider.GetRequiredService<UserStatisticsService>();
            return await UpdateLastActivityTimestampForUserAsync(userStatistics, guildUser);
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> UserJoinedAsync(SocketGuildUser user)
        {
            if (user.IsBot || user.IsWebhook)
            {
                return OperationResult.FromSuccess();
            }

            using var eventScope = this.Services.CreateScope();
            var userStatistics = eventScope.ServiceProvider.GetRequiredService<UserStatisticsService>();
            return await UpdateLastActivityTimestampForUserAsync(userStatistics, user);
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> ReactionAddedAsync
        (
            Cacheable<IUserMessage, ulong> message,
            ISocketMessageChannel channel,
            SocketReaction reaction
        )
        {
            var reactingUser = await channel.GetUserAsync(reaction.UserId);
            if (reactingUser.IsBot || reactingUser.IsWebhook)
            {
                return OperationResult.FromSuccess();
            }

            if (!(reactingUser is IGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            using var eventScope = this.Services.CreateScope();
            var userStatistics = eventScope.ServiceProvider.GetRequiredService<UserStatisticsService>();
            return await UpdateLastActivityTimestampForUserAsync(userStatistics, guildUser);
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> UserVoiceStateUpdatedAsync
        (
            SocketUser user,
            SocketVoiceState oldState,
            SocketVoiceState newState
        )
        {
            if (user.IsBot || user.IsWebhook)
            {
                return OperationResult.FromSuccess();
            }

            if (!(user is IGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            using var eventScope = this.Services.CreateScope();
            var userStatistics = eventScope.ServiceProvider.GetRequiredService<UserStatisticsService>();
            return await UpdateLastActivityTimestampForUserAsync(userStatistics, guildUser);
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> GuildMemberUpdatedAsync
        (
            SocketGuildUser oldMember,
            SocketGuildUser newMember
        )
        {
            if (newMember.IsBot || newMember.IsWebhook)
            {
                return OperationResult.FromSuccess();
            }

            using var eventScope = this.Services.CreateScope();
            var userStatistics = eventScope.ServiceProvider.GetRequiredService<UserStatisticsService>();
            return await UpdateLastActivityTimestampForUserAsync(userStatistics, newMember);
        }

        /// <inheritdoc/>
        protected override async Task<OperationResult> MessageReceivedAsync(SocketMessage message)
        {
            if (message.Author.IsBot || message.Author.IsWebhook)
            {
                return OperationResult.FromSuccess();
            }

            if (!(message.Author is SocketGuildUser guildUser))
            {
                return OperationResult.FromSuccess();
            }

            if (!(message.Channel is SocketTextChannel textChannel))
            {
                return OperationResult.FromSuccess();
            }

            // First, let's get some valid service instances
            using var eventScope = this.Services.CreateScope();
            var autoroles = eventScope.ServiceProvider.GetRequiredService<AutoroleService>();

            var autorolesOnServer = (await autoroles.GetAutorolesAsync(guildUser.Guild)).ToList();

            var wantsToUpdateChannelMessageCounts =
                autorolesOnServer.Any(a => a.Conditions.Any(c => c is MessageCountInChannelCondition));

            var wantsToUpdateServerMessageCounts =
                autorolesOnServer.Any(a => a.Conditions.Any(c => c is MessageCountInGuildCondition));

            var wantsToUpdateLastActivityTime =
                autorolesOnServer.Any(a => a.Conditions.Any(c => c is TimeSinceLastActivityCondition));

            if
            (
                !wantsToUpdateChannelMessageCounts &&
                !wantsToUpdateServerMessageCounts &&
                !wantsToUpdateLastActivityTime
            )
            {
                return OperationResult.FromSuccess();
            }

            var userStatistics = eventScope.ServiceProvider.GetRequiredService<UserStatisticsService>();
            var updateResult = await UpdateLastActivityTimestampForUserAsync(userStatistics, guildUser);
            if (!updateResult.IsSuccess)
            {
                return OperationResult.FromError(updateResult);
            }

            if (wantsToUpdateChannelMessageCounts)
            {
                var getChannelStats = await userStatistics.GetOrCreateUserChannelStatisticsAsync
                (
                    guildUser,
                    textChannel
                );

                if (!getChannelStats.IsSuccess)
                {
                    this.Log.LogError(getChannelStats.Exception, getChannelStats.ErrorReason);
                    return OperationResult.FromError(getChannelStats);
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
                    else if (!(countResult.Exception is null))
                    {
                        this.Log.LogError(countResult.Exception, "Message counting failed.");
                        return OperationResult.FromError(countResult);
                    }
                }
            }

            if (wantsToUpdateServerMessageCounts)
            {
                var getGlobalStats = await userStatistics.GetOrCreateUserServerStatisticsAsync(guildUser);
                if (!getGlobalStats.IsSuccess)
                {
                    this.Log.LogError(getGlobalStats.Exception, getGlobalStats.ErrorReason);
                    return OperationResult.FromError(getGlobalStats);
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
                            if (!(countResult.Exception is null))
                            {
                                this.Log.LogError(countResult.Exception, "Message counting failed.");
                                return OperationResult.FromError(countResult);
                            }

                            continue;
                        }

                        sum += await countResult.Entity;
                    }

                    globalStats.TotalMessageCount = sum;
                }
            }

            userStatistics.SaveChanges();
            return OperationResult.FromSuccess();
        }

        /// <summary>
        /// Updates the last activity timestamp for the given user.
        /// </summary>
        /// <param name="userStatistics">The statistics service.</param>
        /// <param name="guildUser">The guild user.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task<OperationResult> UpdateLastActivityTimestampForUserAsync
        (
            UserStatisticsService userStatistics,
            IGuildUser guildUser
        )
        {
            var getGlobalStats = await userStatistics.GetOrCreateUserServerStatisticsAsync(guildUser);
            if (!getGlobalStats.IsSuccess)
            {
                this.Log.LogError(getGlobalStats.Exception, getGlobalStats.ErrorReason);
                return OperationResult.FromError(getGlobalStats);
            }

            var globalStats = getGlobalStats.Entity;
            globalStats.LastActivityTime = DateTimeOffset.UtcNow;

            return OperationResult.FromSuccess();
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

                // We'll explicitly include the latest message, since it'd get ignored otherwise
                if (latestMessage.Author.Id == user.Id)
                {
                    sum += 1;
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
                            if (latestMessage.Author.Id == user.Id)
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
