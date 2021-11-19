//
//  ServerService.cs
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
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Core.Services.Servers;

/// <summary>
/// Handles modification of server settings.
/// </summary>
public sealed class ServerService
{
    private readonly CoreDatabaseContext _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerService"/> class.
    /// </summary>
    /// <param name="database">The core database.</param>
    public ServerService(CoreDatabaseContext database)
    {
        _database = database;
    }

    /// <summary>
    /// Determines whether or not a Discord server is stored in the database.
    /// </summary>
    /// <param name="discordServer">The Discord server.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns><value>true</value> if the server is stored; otherwise, <value>false</value>.</returns>
    [Pure]
    public async Task<bool> IsServerKnownAsync
    (
        Snowflake discordServer,
        CancellationToken ct = default
    )
    {
        var hasServer = await _database.Servers.ServersideQueryAsync
        (
            q => q
                .Where(s => s.DiscordID == discordServer)
                .AnyAsync(ct)
        );

        return hasServer;
    }

    /// <summary>
    /// Gets an existing set of information about a Discord server, or registers it with the database if one is not found.
    /// </summary>
    /// <param name="discordServer">The Discord server.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>Stored information about the server.</returns>
    public async Task<Result<Server>> GetOrRegisterServerAsync
    (
        Snowflake discordServer,
        CancellationToken ct = default
    )
    {
        if (!await IsServerKnownAsync(discordServer, ct))
        {
            return await AddServerAsync(discordServer, ct);
        }

        return await GetServerAsync(discordServer, ct);
    }

    /// <summary>
    /// Gets a stored server from the database that matches the given Discord server.
    /// </summary>
    /// <param name="discordServer">The Discord server.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>Stored information about the server.</returns>
    [Pure]
    public async Task<Result<Server>> GetServerAsync
    (
        Snowflake discordServer,
        CancellationToken ct = default
    )
    {
        var server = await _database.Servers.ServersideQueryAsync
        (
            q => q
                .Where(u => u.DiscordID == discordServer)
                .SingleOrDefaultAsync(ct)
        );

        if (server is not null)
        {
            return server;
        }

        return new UserError("That server has not been registered in the database.");
    }

    /// <summary>
    /// Adds a Discord server to the database.
    /// </summary>
    /// <param name="discordServer">The Discord server.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>The freshly created information about the server.</returns>
    /// <exception cref="ArgumentException">Thrown if the server already exists in the database.</exception>
    public async Task<Result<Server>> AddServerAsync
    (
        Snowflake discordServer,
        CancellationToken ct = default
    )
    {
        if (await IsServerKnownAsync(discordServer, ct))
        {
            return new UserError
            (
                $"A server with the ID {discordServer} has already been added to the database."
            );
        }

        var server = _database.CreateProxy<Server>(discordServer);
        _database.Servers.Update(server);

        server.IsNSFW = true;

        await _database.SaveChangesAsync(ct);

        return Result<Server>.FromSuccess(server);
    }

    /// <summary>
    /// Gets the description of the server.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public Result<string> GetDescription(Server server)
    {
        return server.Description.IsNullOrWhitespace()
            ? new UserError("No description set.")
            : server.Description;
    }

    /// <summary>
    /// Sets the description of the server.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="description">The new description.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetDescriptionAsync
    (
        Server server,
        string description,
        CancellationToken ct = default
    )
    {
        description = description.Trim();

        if (description.IsNullOrWhitespace())
        {
            return new UserError
            (
                "The description must not be empty."
            );
        }

        if (server.Description == description)
        {
            return new UserError
            (
                "That's already the server's description."
            );
        }

        if (description.Length > 800)
        {
            return new UserError
            (
                "The description may not be longer than 800 characters."
            );
        }

        server.Description = description;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Gets the server's join message.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <returns>A retrieval result which may or may not have succeeded.</returns>
    [Pure]
    public Result<string> GetJoinMessage(Server server)
    {
        return server.JoinMessage.IsNullOrWhitespace()
            ? new UserError("No join message set.")
            : server.JoinMessage;
    }

    /// <summary>
    /// Sets the server's join message.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="joinMessage">The new join message.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetJoinMessageAsync
    (
        Server server,
        string joinMessage,
        CancellationToken ct = default
    )
    {
        joinMessage = joinMessage.Trim();

        if (joinMessage.IsNullOrWhitespace())
        {
            return new UserError
            (
                "The join message must not be empty."
            );
        }

        if (server.JoinMessage == joinMessage)
        {
            return new UserError
            (
                "That's already the server's join message."
            );
        }

        if (joinMessage.Length > 1200)
        {
            return new UserError
            (
                "The join message may not be longer than 1200 characters."
            );
        }

        server.JoinMessage = joinMessage;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets a value indicating whether the server is NSFW.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="isNsfw">Whether or not the server is NSFW.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetIsNSFWAsync
    (
        Server server,
        bool isNsfw,
        CancellationToken ct = default
    )
    {
        if (server.IsNSFW == isNsfw)
        {
            return new UserError
            (
                $"The server is already {(isNsfw ? string.Empty : "not")} NSFW."
            );
        }

        server.IsNSFW = isNsfw;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Sets a value indicating whether the server should send first-join messages.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="sendJoinMessage">Whether or not the server should send join messages.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> SetSendJoinMessageAsync
    (
        Server server,
        bool sendJoinMessage,
        CancellationToken ct = default
    )
    {
        if (server.SendJoinMessage == sendJoinMessage)
        {
            return new UserError
            (
                $"The server already {(sendJoinMessage ? string.Empty : "not")} sending first-join messages."
            );
        }

        server.SendJoinMessage = sendJoinMessage;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }

    /// <summary>
    /// Clears the first-join message, if one is set.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="ct">The cancellation token in use.</param>
    /// <returns>A modification result which may or may not have succeeded.</returns>
    public async Task<Result> ClearJoinMessageAsync(Server server, CancellationToken ct = default)
    {
        if (server.JoinMessage is null)
        {
            return new UserError("No join message has been set.");
        }

        server.JoinMessage = null;
        await _database.SaveChangesAsync(ct);

        return Result.FromSuccess();
    }
}
