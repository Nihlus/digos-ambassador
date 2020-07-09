//
//  MessageCountInGuildCondition.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions
{
    /// <summary>
    /// Represents a condition that requires a certain number of messages in the whole server.
    /// </summary>
    public class MessageCountInGuildCondition : MessageCountInSourceCondition<MessageCountInGuildCondition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInGuildCondition"/> class.
        /// </summary>
        /// <param name="sourceID">The source ID.</param>
        /// <param name="requiredCount">The required message count.</param>
        [UsedImplicitly]
        protected MessageCountInGuildCondition(long sourceID, long requiredCount)
            : base(sourceID, requiredCount)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MessageCountInGuildCondition"/> class.
        /// </summary>
        /// <param name="guild">The source guild.</param>
        /// <param name="requiredCount">The required number of messages.</param>
        public MessageCountInGuildCondition(IGuild guild, long requiredCount)
            : this((long)guild.Id, requiredCount)
        {
        }

        /// <inheritdoc/>
        public override string GetDescriptiveUIText()
        {
            return $"{this.RequiredCount} messages in the server";
        }

        /// <inheritdoc/>
        public override async Task<RetrieveEntityResult<bool>> IsConditionFulfilledForUserAsync
        (
            IServiceProvider services,
            IGuildUser discordUser,
            CancellationToken ct = default
        )
        {
            var statistics = services.GetRequiredService<UserStatisticsService>();

            var getUserStatistics = await statistics.GetOrCreateUserServerStatisticsAsync(discordUser, ct);
            if (!getUserStatistics.IsSuccess)
            {
                return RetrieveEntityResult<bool>.FromError(getUserStatistics);
            }

            var userStatistics = getUserStatistics.Entity;

            return userStatistics.TotalMessageCount.HasValue && userStatistics.TotalMessageCount >= this.RequiredCount;
        }
    }
}
