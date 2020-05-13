//
//  TimeSinceLastActivityCondition.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using Discord;
using Humanizer;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions
{
    /// <summary>
    /// Represents a requirement for an elapsed time since the user joined.
    /// </summary>
    public class TimeSinceLastActivityCondition : TimeSinceEventCondition<TimeSinceLastActivityCondition>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSinceLastActivityCondition"/> class.
        /// </summary>
        /// <param name="requiredTime">The required time.</param>
        public TimeSinceLastActivityCondition(TimeSpan requiredTime)
            : base(requiredTime)
        {
        }

        /// <inheritdoc />
        public override string GetDescriptiveUIText()
        {
            return $"Has been active in the last {this.RequiredTime.Humanize(toWords: true, precision: 3)}";
        }

        /// <inheritdoc />
        public override async Task<bool> IsConditionFulfilledForUser(IServiceProvider services, IGuildUser discordUser)
        {
            var statistics = services.GetRequiredService<UserStatisticsService>();

            var getUserStatistics = await statistics.GetOrCreateUserServerStatisticsAsync(discordUser);
            if (!getUserStatistics.IsSuccess)
            {
                // TODO: Maybe we should throw here instead, or return a monad?
                return false;
            }

            var userStatistics = getUserStatistics.Entity;

            if (userStatistics.LastActivityTime is null)
            {
                // The user has never been active
                return false;
            }

            return (DateTime.UtcNow - userStatistics.LastActivityTime) >= this.RequiredTime;
        }
    }
}
