//
//  RoleplayLoggingResponder.cs
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
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Gateway.Responders;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Responders
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
        private readonly IDiscordRestChannelAPI _channelAPI;
        private readonly ILogger<RoleplayLoggingResponder> _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayLoggingResponder"/> class.
        /// </summary>
        /// <param name="roleplays">The roleplay service.</param>
        /// <param name="channelAPI">The channel API.</param>
        /// <param name="log">The logging instance.</param>
        public RoleplayLoggingResponder
        (
            RoleplayDiscordService roleplays,
            IDiscordRestChannelAPI channelAPI,
            ILogger<RoleplayLoggingResponder> log
        )
        {
            _roleplays = roleplays;
            _channelAPI = channelAPI;
            _log = log;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IReady gatewayEvent, CancellationToken ct = default)
        {
            var activeRoleplays = await _roleplays.QueryRoleplaysAsync
            (
                q => q
                    .Where(rp => rp.IsActive)
                    .Where(rp => rp.DedicatedChannelID.HasValue)
            );

            foreach (var activeRoleplay in activeRoleplays)
            {
                var ensureLogged = await _roleplays.EnsureAllMessagesAreLoggedAsync(activeRoleplay);
                if (!ensureLogged.IsSuccess)
                {
                    return Result.FromError(ensureLogged);
                }

                var updatedMessages = ensureLogged.Entity;

                if (updatedMessages > 0)
                {
                    _log.LogInformation
                    (
                        "Added or updated {UpdateCount} missed {Pluralized} in \"{RoleplayName}\"",
                        updatedMessages,
                        updatedMessages > 1 ? "messages" : "message",
                        activeRoleplay.Name
                    );
                }
            }

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
        {
            var isBot = gatewayEvent.Author.IsBot.HasValue && gatewayEvent.Author.IsBot.Value;
            var isSystem = gatewayEvent.Author.IsSystem.HasValue && gatewayEvent.Author.IsSystem.Value;
            if (isBot || isSystem)
            {
                return Result.FromSuccess();
            }

            return await _roleplays.ConsumeMessageAsync(gatewayEvent);
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageUpdate gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.Author.HasValue)
            {
                return Result.FromSuccess();
            }

            if (gatewayEvent.Author.Value.IsBot.Value || gatewayEvent.Author.Value.IsSystem.Value)
            {
                return Result.FromSuccess();
            }

            // Ignore all changes except text changes
            var isTextUpdate = gatewayEvent.EditedTimestamp.HasValue &&
                               gatewayEvent.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes();

            if (!isTextUpdate)
            {
                return Result.FromSuccess();
            }

            return await _roleplays.ConsumeMessageAsync(gatewayEvent);
        }
    }
}
