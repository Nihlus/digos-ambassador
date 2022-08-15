//
//  UserStatisticsService.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Model.Statistics;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Services;

/// <summary>
/// Business logic class for user statistics.
/// </summary>
[PublicAPI]
public sealed class UserStatisticsService
{
    private readonly AutoroleDatabaseContext _database;
    private readonly UserService _users;
    private readonly ServerService _servers;

    /// <summary>
    /// Initializes a new instance of the <see cref="UserStatisticsService"/> class.
    /// </summary>
    /// <param name="database">The database.</param>
    /// <param name="users">The user service.</param>
    /// <param name="servers">The server service.</param>
    public UserStatisticsService
    (
        AutoroleDatabaseContext database,
        UserService users,
        ServerService servers
    )
    {
        _database = database;
        _users = users;
        _servers = servers;
    }

    /// <summary>
    /// Gets or creates a set of statistics for a given user.
    /// </summary>
    /// <param name="discordUserID">The user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A creation result which may or may not have succeeded.</returns>
    public async Task<Result<UserStatistics>> GetOrCreateUserStatisticsAsync
    (
        Snowflake discordUserID,
        CancellationToken ct = default
    )
    {
        var statistics = await _database.UserStatistics.ServersideQueryAsync
        (
            q => q
                .Where(s => s.User.DiscordID == discordUserID)
                .SingleOrDefaultAsync(ct)
        );

        if (statistics is not null)
        {
            return statistics;
        }

        var getUser = await _users.GetOrRegisterUserAsync(discordUserID, ct);
        if (!getUser.IsSuccess)
        {
            return Result<UserStatistics>.FromError(getUser);
        }

        var user = getUser.Entity;

        var newStatistics = _database.CreateProxy<UserStatistics>(user);

        _database.UserStatistics.Update(newStatistics);
        await _database.SaveChangesAsync(ct);

        return newStatistics;
    }

    /// <summary>
    /// Gets or creates a set of per-server statistics for a given user.
    /// </summary>
    /// <param name="guildID">The ID of the guild the user is on.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A creation result which may or may not have succeeded.</returns>
    public async Task<Result<UserServerStatistics>> GetOrCreateUserServerStatisticsAsync
    (
        Snowflake guildID,
        Snowflake userID,
        CancellationToken ct = default
    )
    {
        var getStatistics = await GetOrCreateUserStatisticsAsync(userID, ct);
        if (!getStatistics.IsSuccess)
        {
            return Result<UserServerStatistics>.FromError(getStatistics);
        }

        var statistics = getStatistics.Entity;
        var existingServerStatistics = statistics.ServerStatistics.FirstOrDefault
        (
            s => s.Server.DiscordID == guildID
        );

        if (existingServerStatistics is not null)
        {
            return existingServerStatistics;
        }

        var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
        if (!getServer.IsSuccess)
        {
            return Result<UserServerStatistics>.FromError(getServer);
        }

        var server = getServer.Entity;
        server = _database.NormalizeReference(server);

        var newServerStatistics = _database.CreateProxy<UserServerStatistics>(server);

        _database.Update(newServerStatistics);
        statistics.ServerStatistics.Add(newServerStatistics);

        await _database.SaveChangesAsync(ct);

        return newServerStatistics;
    }

    /// <summary>
    /// Gets or creates a set of per-channel statistics for a given user.
    /// </summary>
    /// <param name="guildID">The ID of the guild the user is on.</param>
    /// <param name="userID">The ID of the user.</param>
    /// <param name="channelID">The ID of the channel.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A creation result which may or may not have succeeded.</returns>
    public async Task<Result<UserChannelStatistics>> GetOrCreateUserChannelStatisticsAsync
    (
        Snowflake guildID,
        Snowflake userID,
        Snowflake channelID,
        CancellationToken ct = default
    )
    {
        var getServerStats = await GetOrCreateUserServerStatisticsAsync(guildID, userID, ct);
        if (!getServerStats.IsSuccess)
        {
            return Result<UserChannelStatistics>.FromError(getServerStats);
        }

        var serverStats = getServerStats.Entity;
        var existingStats = serverStats.ChannelStatistics.FirstOrDefault
        (
            s => s.ChannelID == channelID
        );

        if (existingStats is not null)
        {
            return existingStats;
        }

        var newStats = _database.CreateProxy<UserChannelStatistics>(channelID);

        _database.Update(newStats);
        serverStats.ChannelStatistics.Add(newStats);

        await _database.SaveChangesAsync(ct);

        return newStats;
    }

    /// <summary>
    /// Sets the message count for the given channel statistic entity.
    /// </summary>
    /// <param name="channelStats">The channel statistics.</param>
    /// <param name="channelMessageCount">The new message count.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetChannelMessageCountAsync
    (
        UserChannelStatistics channelStats,
        long? channelMessageCount,
        CancellationToken ct = default
    )
    {
        channelStats.MessageCount = channelMessageCount;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets the message count for the given server statistic entity.
    /// </summary>
    /// <param name="globalStats">The server statistics.</param>
    /// <param name="totalMessageCount">The new message count.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetTotalMessageCountAsync
    (
        UserServerStatistics globalStats,
        long? totalMessageCount,
        CancellationToken ct = default
    )
    {
        globalStats.TotalMessageCount = totalMessageCount;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Updates the timestamp of the given server statistic entity to the current time.
    /// </summary>
    /// <param name="globalStats">The server statistics.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> UpdateTimestampAsync
    (
        UserServerStatistics globalStats,
        CancellationToken ct = default
    )
    {
        var now = DateTimeOffset.UtcNow;
        if (globalStats.LastActivityTime == now)
        {
            return new UserError("That's already the latest timestamp.");
        }

        if (globalStats.LastActivityTime > now)
        {
            return new UserError("That timestamp is earlier than the current timestamp.");
        }

        globalStats.LastActivityTime = now;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }
}
