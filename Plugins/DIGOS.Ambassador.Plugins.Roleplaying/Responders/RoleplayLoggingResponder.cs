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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Humanizer;
using JetBrains.Annotations;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Behaviours
{
    /// <summary>
    /// Acts on user messages, logging them into an active roleplay if relevant.
    /// </summary>
    [UsedImplicitly]
    internal sealed class RoleplayLoggingResponder :
        IResponder<IReady>,
        IResponder<IMessageCreate>,
        IResponder<IMessageUpdate>
    {
        private readonly RoleplayDiscordService _roleplays;

        public RoleplayLoggingResponder(RoleplayDiscordService roleplays)
        {
            _roleplays = roleplays;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
        {
            foreach (var guild in this.Client.Guilds)
            {
                var getRoleplays = await _roleplays.GetRoleplaysAsync(guild);
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
                        var updateResult = await _roleplays.ConsumeMessageAsync(userMessage);
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

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
        {
            if (!(arg is SocketUserMessage message))
            {
                return new UserError("The message was not a user message.");
            }

            if (arg.Author.IsBot || arg.Author.IsWebhook)
            {
                return Result.FromSuccess();
            }

            var discard = 0;

            if (message.HasCharPrefix('!', ref discard))
            {
                return Result.FromSuccess();
            }

            if (message.HasMentionPrefix(this.Client.CurrentUser, ref discard))
            {
                return Result.FromSuccess();
            }

            var consumeResult = await _roleplays.ConsumeMessageAsync(message);
            if (!consumeResult.IsSuccess)
            {
                return Result.FromError(consumeResult);
            }

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageUpdate gatewayEvent, CancellationToken ct = default)
        {
            // Ignore all changes except text changes
            var isTextUpdate = updatedMessage.EditedTimestamp.HasValue &&
                               updatedMessage.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes();

            if (!isTextUpdate)
            {
                return Result.FromSuccess();
            }

            return await MessageReceivedAsync(updatedMessage);
        }
    }
}
