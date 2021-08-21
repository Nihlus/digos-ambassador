//
//  UserActivityResponder.cs
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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Remora.Discord.API.Abstractions.Gateway.Events;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using static System.Transactions.IsolationLevel;

namespace DIGOS.Ambassador.Plugins.Autorole.Responders
{
    /// <summary>
    /// Responds to anything that qualifies as a user being active.
    /// </summary>
    public class UserActivityResponder :
        IResponder<IMessageCreate>,
        IResponder<IMessageReactionAdd>,
        IResponder<IMessageReactionRemove>,
        IResponder<ITypingStart>,
        IResponder<IVoiceStateUpdate>,
        IResponder<IPresenceUpdate>
    {
        private readonly AutoroleService _autoroles;
        private readonly AutoroleUpdateService _autoroleUpdates;
        private readonly UserStatisticsService _statistics;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserActivityResponder"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="autoroleUpdates">The autorole update service.</param>
        /// <param name="statistics">The statistics service.</param>
        public UserActivityResponder
        (
            AutoroleService autoroles,
            AutoroleUpdateService autoroleUpdates,
            UserStatisticsService statistics
        )
        {
            _autoroles = autoroles;
            _autoroleUpdates = autoroleUpdates;
            _statistics = statistics;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.IsDefined(out var guildID))
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.Author.ID;

            return await UpdateTimestampAndRelevantAutorolesAsync(ct, guildID, user);
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageReactionAdd gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.IsDefined(out var guildID))
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.UserID;

            return await UpdateTimestampAndRelevantAutorolesAsync(ct, guildID, user);
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageReactionRemove gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.IsDefined(out var guildID))
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.UserID;

            return await UpdateTimestampAndRelevantAutorolesAsync(ct, guildID, user);
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(ITypingStart gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.IsDefined(out var guildID))
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.UserID;

            return await UpdateTimestampAndRelevantAutorolesAsync(ct, guildID, user);
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IVoiceStateUpdate gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.IsDefined(out var guildID))
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.UserID;

            return await UpdateTimestampAndRelevantAutorolesAsync(ct, guildID, user);
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IPresenceUpdate gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.User.ID.IsDefined(out var guildID))
            {
                return Result.FromSuccess();
            }

            var user = gatewayEvent.User.ID.Value;

            return await UpdateTimestampAndRelevantAutorolesAsync(ct, guildID, user);
        }

        private async Task<Result> UpdateTimestampAndRelevantAutorolesAsync(CancellationToken ct, Snowflake guild, Snowflake user)
        {
            {
                using var timestampTransaction = TransactionFactory.Create(ReadCommitted);

                var getServerStatistics = await _statistics.GetOrCreateUserServerStatisticsAsync(guild, user, ct);
                if (!getServerStatistics.IsSuccess)
                {
                    return Result.FromError(getServerStatistics);
                }

                var serverStatistics = getServerStatistics.Entity;

                var updateTimestamp = await _statistics.UpdateTimestampAsync(serverStatistics, ct);
                if (!updateTimestamp.IsSuccess)
                {
                    return updateTimestamp;
                }

                timestampTransaction.Complete();
            }

            var autoroles = await _autoroles.GetAutorolesAsync
            (
                guild,
                q => q
                    .Where(a => a.IsEnabled)
                    .Where
                    (
                        a => a.Conditions.Any
                        (
                            c =>
                                c.GetType() == typeof(TimeSinceLastActivityCondition) ||
                                c.GetType() == typeof(TimeSinceJoinCondition)
                        )
                    ),
                ct
            );

            foreach (var autorole in autoroles)
            {
                using var transaction = TransactionFactory.Create();

                var updateAutorole = await _autoroleUpdates.UpdateAutoroleForUserAsync(autorole, guild, user, ct);
                if (!updateAutorole.IsSuccess)
                {
                    return Result.FromError(updateAutorole);
                }

                transaction.Complete();
            }

            return Result.FromSuccess();
        }
    }
}
