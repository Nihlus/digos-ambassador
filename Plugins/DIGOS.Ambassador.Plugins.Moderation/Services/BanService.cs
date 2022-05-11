//
//  BanService.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Microsoft.EntityFrameworkCore;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services;

/// <summary>
/// Acts as an interface for accessing and modifying bans.
/// </summary>
public sealed class BanService
{
    private readonly ModerationDatabaseContext _database;
    private readonly ServerService _servers;
    private readonly UserService _users;

    /// <summary>
    /// Initializes a new instance of the <see cref="BanService"/> class.
    /// </summary>
    /// <param name="database">The database context.</param>
    /// <param name="servers">The server service.</param>
    /// <param name="users">The user service.</param>
    public BanService
    (
        ModerationDatabaseContext database,
        ServerService servers,
        UserService users
    )
    {
        _database = database;
        _servers = servers;
        _users = users;
    }

    /// <summary>
    /// Gets the bans issued on the given guild.
    /// </summary>
    /// <param name="guildID">The ID of the guild the ban is on.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>The warnings.</returns>
    public Task<IReadOnlyList<UserBan>> GetBansAsync(Snowflake guildID, CancellationToken ct = default)
    {
        return _database.UserBans.ServersideQueryAsync
        (
            q => q.Where(b => b.Server.DiscordID == guildID),
            ct
        );
    }

    /// <summary>
    /// Gets all expired bans.
    /// </summary>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>The warnings.</returns>
    public Task<IReadOnlyList<UserBan>> GetExpiredBansAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return _database.UserBans.ServersideQueryAsync
        (
            q => q.Where
            (
                b => b.ExpiresOn.HasValue && now >= b.ExpiresOn.Value
            ),
            ct
        );
    }

    /// <summary>
    /// Retrieves a ban with the given ID from the database.
    /// </summary>
    /// <param name="guildID">The ID of the guild the ban is on.</param>
    /// <param name="banID">The ban ID.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    public async Task<Result<UserBan>> GetBanAsync
    (
        Snowflake guildID,
        long banID,
        CancellationToken ct = default
    )
    {
        // The server isn't strictly required here, but it prevents leaking bans between servers.
        var ban = await _database.UserBans.ServersideQueryAsync
        (
            q => q.SingleOrDefaultAsync
            (
                n => n.ID == banID && n.Server.DiscordID == guildID,
                ct
            )
        );

        if (ban is null)
        {
            return new UserError("There's no ban with that ID in the database.");
        }

        return ban;
    }

    /// <summary>
    /// Creates a ban for the given user.
    /// </summary>
    /// <param name="authorID">The ID of the author of the warning.</param>
    /// <param name="userID">The ID of the warned user.</param>
    /// <param name="guildID">The ID of the guild the user is on.</param>
    /// <param name="reason">The reason of the ban.</param>
    /// <param name="messageID">The message that caused the ban, if any.</param>
    /// <param name="expiresOn">The expiry date for the ban, if any.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A creation result which may or may not have succeeded.</returns>
    public async Task<Result<UserBan>> CreateBanAsync
    (
        Snowflake authorID,
        Snowflake userID,
        Snowflake guildID,
        string reason,
        Snowflake? messageID = null,
        DateTimeOffset? expiresOn = null,
        CancellationToken ct = default
    )
    {
        var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
        if (!getServer.IsSuccess)
        {
            return Result<UserBan>.FromError(getServer);
        }

        var server = getServer.Entity;

        var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
        if (!getUser.IsSuccess)
        {
            return Result<UserBan>.FromError(getUser);
        }

        var user = getUser.Entity;

        var getAuthor = await _users.GetOrRegisterUserAsync(authorID, ct);
        if (!getAuthor.IsSuccess)
        {
            return Result<UserBan>.FromError(getAuthor);
        }

        var author = getAuthor.Entity;

        var ban = _database.CreateProxy<UserBan>(server, user, author, string.Empty, null!, null!);
        _database.UserBans.Update(ban);

        var setReason = await SetBanReasonAsync(ban, reason, ct);
        if (!setReason.IsSuccess)
        {
            return Result<UserBan>.FromError(setReason);
        }

        if (messageID is not null)
        {
            var setMessage = await SetBanContextMessageAsync(ban, messageID.Value, ct);
            if (!setMessage.IsSuccess)
            {
                return Result<UserBan>.FromError(setMessage);
            }
        }

        if (expiresOn is not null)
        {
            var setExpiry = await SetBanExpiryDateAsync(ban, expiresOn.Value, ct);
            if (!setExpiry.IsSuccess)
            {
                return Result<UserBan>.FromError(setExpiry);
            }
        }

        await _database.SaveChangesAsync(ct);

        return ban;
    }

    /// <summary>
    /// Sets the reasons of the given ban.
    /// </summary>
    /// <param name="ban">The ban.</param>
    /// <param name="reason">The reason.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetBanReasonAsync
    (
        UserBan ban,
        string reason,
        CancellationToken ct = default
    )
    {
        reason = reason.Trim();

        if (reason.IsNullOrWhitespace())
        {
            return new UserError("You must provide some reason for the ban.");
        }

        if (reason.Length > 1024)
        {
            return new UserError
            (
                "The ban is too long. It can be at most 1024 characters."
            );
        }

        if (ban.Reason == reason)
        {
            return new UserError("That's already the ban's reason.");
        }

        ban.Reason = reason;
        ban.NotifyUpdate();

        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets the contextually relevant message for the ban.
    /// </summary>
    /// <param name="ban">The ban.</param>
    /// <param name="messageID">The message.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetBanContextMessageAsync
    (
        UserBan ban,
        Snowflake messageID,
        CancellationToken ct = default
    )
    {
        if (ban.MessageID == messageID)
        {
            return new UserError("That's already the ban's context message.");
        }

        ban.MessageID = messageID;
        ban.NotifyUpdate();

        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets the date and time at which the ban expires.
    /// </summary>
    /// <param name="ban">The ban.</param>
    /// <param name="expiresOn">The date and time at which the ban expires.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetBanExpiryDateAsync
    (
        UserBan ban,
        DateTimeOffset expiresOn,
        CancellationToken ct = default
    )
    {
        if (ban.ExpiresOn == expiresOn)
        {
            return new UserError("That's already the ban's expiry date.");
        }

        if (expiresOn < DateTimeOffset.UtcNow)
        {
            return new UserError("Bans can't expire in the past.");
        }

        ban.ExpiresOn = expiresOn;
        ban.NotifyUpdate();

        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Deletes the given ban.
    /// </summary>
    /// <param name="ban">The ban to delete.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A deletion result which may or may ban have succeeded.</returns>
    public async Task<Result> DeleteBanAsync
    (
        UserBan ban,
        CancellationToken ct = default
    )
    {
        if (!_database.UserBans.Any(n => n.ID == ban.ID))
        {
            return new UserError
            (
                "That ban isn't in the database. This is probably an error in the bot."
            );
        }

        _database.UserBans.Remove(ban);
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }
}
