//
//  MessageCountConditionResponder.cs
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
using Remora.Discord.Gateway.Responders;
using Remora.Results;
using static System.Transactions.IsolationLevel;

namespace DIGOS.Ambassador.Plugins.Autorole.Responders
{
    /// <summary>
    /// Responds to message creations, updating relevant autoroles.
    /// </summary>
    public class MessageCountConditionResponder : IResponder<IMessageCreate>
    {
        private readonly AutoroleService _autoroles;
        private readonly AutoroleUpdateService _autoroleUpdates;
        private readonly UserStatisticsService _statistics;

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountConditionResponder"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="statistics">The statistics service.</param>
        /// <param name="autoroleUpdates">The autorole update service.</param>
        public MessageCountConditionResponder
        (
            AutoroleService autoroles,
            UserStatisticsService statistics,
            AutoroleUpdateService autoroleUpdates
        )
        {
            _autoroles = autoroles;
            _statistics = statistics;
            _autoroleUpdates = autoroleUpdates;
        }

        /// <inheritdoc />
        public async Task<Result> RespondAsync(IMessageCreate gatewayEvent, CancellationToken ct = default)
        {
            if (!gatewayEvent.GuildID.HasValue)
            {
                return Result.FromSuccess();
            }

            using var transaction = TransactionFactory.Create();

            var user = gatewayEvent.Author.ID;
            var guild = gatewayEvent.GuildID.Value;

            var getUserStatistics = await _statistics.GetOrCreateUserServerStatisticsAsync(guild, user, ct);
            if (!getUserStatistics.IsSuccess)
            {
                return Result.FromError(getUserStatistics);
            }

            var userStatistics = getUserStatistics.Entity;
            var setTotalCount = await _statistics.SetTotalMessageCountAsync
            (
                userStatistics,
                (userStatistics.TotalMessageCount ?? 0) + 1,
                ct
            );

            if (!setTotalCount.IsSuccess)
            {
                return setTotalCount;
            }

            var channel = gatewayEvent.ChannelID;
            var getChannelStatistics = await _statistics.GetOrCreateUserChannelStatisticsAsync
            (
                guild,
                user,
                channel,
                ct
            );

            if (!getChannelStatistics.IsSuccess)
            {
                return Result.FromError(getChannelStatistics);
            }

            var channelStatistics = getChannelStatistics.Entity;
            var setChannelCount = await _statistics.SetChannelMessageCountAsync
            (
                channelStatistics,
                (channelStatistics.MessageCount ?? 0) + 1,
                ct
            );

            if (!setChannelCount.IsSuccess)
            {
                return setChannelCount;
            }

            // Finally, update the relevant autoroles
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
                            c.GetType() == typeof(MessageCountInGuildCondition) ||
                            c.GetType() == typeof(MessageCountInChannelCondition)
                    )
                ),
                ct
            );

            foreach (var autorole in autoroles)
            {
                var updateAutorole = await _autoroleUpdates.UpdateAutoroleForUserAsync(autorole, guild, user, ct);
                if (!updateAutorole.IsSuccess)
                {
                    return Result.FromError(updateAutorole);
                }
            }

            transaction.Complete();
            return Result.FromSuccess();
        }
    }
}
