//
//  TimeSinceJoinCondition.cs
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
using Humanizer;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;

/// <summary>
/// Represents a requirement for an elapsed time since the user joined.
/// </summary>
public class TimeSinceJoinCondition : TimeSinceEventCondition<TimeSinceJoinCondition>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeSinceJoinCondition"/> class.
    /// </summary>
    /// <param name="requiredTime">The required time.</param>
    public TimeSinceJoinCondition(TimeSpan requiredTime)
        : base(requiredTime)
    {
    }

    /// <inheritdoc />
    public override string GetDescriptiveUIText()
    {
        return $"Has been in the server for at least {this.RequiredTime.Humanize(toWords: true, precision: 3)}";
    }

    /// <inheritdoc/>
    public override async Task<Result<bool>> IsConditionFulfilledForUserAsync
    (
        IServiceProvider services,
        Snowflake guildID,
        Snowflake userID,
        CancellationToken ct = default
    )
    {
        var guildAPI = services.GetRequiredService<IDiscordRestGuildAPI>();
        var getMember = await guildAPI.GetGuildMemberAsync(guildID, userID, ct);
        if (!getMember.IsSuccess)
        {
            return Result<bool>.FromError(getMember);
        }

        var member = getMember.Entity;

        var timeSinceJoin = DateTimeOffset.UtcNow - member.JoinedAt.UtcDateTime;
        return timeSinceJoin >= this.RequiredTime;
    }
}
