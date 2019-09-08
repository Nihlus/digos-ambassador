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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Core.Services.Servers
{
    /// <summary>
    /// Handles modification of server settings.
    /// </summary>
    [PublicAPI]
    public sealed class ServerService
    {
        [NotNull]
        private readonly CoreDatabaseContext _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerService"/> class.
        /// </summary>
        /// <param name="database">The core database.</param>
        public ServerService([NotNull] CoreDatabaseContext database)
        {
            _database = database;
        }

        /// <summary>
        /// Determines whether or not a Discord server is stored in the database.
        /// </summary>
        /// <param name="discordServer">The Discord server.</param>
        /// <returns><value>true</value> if the server is stored; otherwise, <value>false</value>.</returns>
        [Pure, NotNull]
        public async Task<bool> IsServerKnownAsync([NotNull] IGuild discordServer)
        {
            return await _database.Servers.AnyAsync(u => u.DiscordID == (long)discordServer.Id);
        }

        /// <summary>
        /// Gets an existing set of information about a Discord server, or registers it with the database if one is not found.
        /// </summary>
        /// <param name="discordServer">The Discord server.</param>
        /// <returns>Stored information about the server.</returns>
        [NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Server>> GetOrRegisterServerAsync([NotNull] IGuild discordServer)
        {
            if (!await IsServerKnownAsync(discordServer))
            {
                return await AddServerAsync(discordServer);
            }

            return await GetServerAsync(discordServer);
        }

        /// <summary>
        /// Gets a stored server from the database that matches the given Discord server.
        /// </summary>
        /// <param name="discordServer">The Discord server.</param>
        /// <returns>Stored information about the server.</returns>
        [Pure, NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Server>> GetServerAsync([NotNull] IGuild discordServer)
        {
            var server = await _database.Servers.FirstOrDefaultAsync(u => u.DiscordID == (long)discordServer.Id);
            if (server is null)
            {
                return RetrieveEntityResult<Server>.FromError("That server has not been registered in the database.");
            }

            return RetrieveEntityResult<Server>.FromSuccess(server);
        }

        /// <summary>
        /// Adds a Discord server to the database.
        /// </summary>
        /// <param name="discordServer">The Discord server.</param>
        /// <returns>The freshly created information about the server.</returns>
        /// <exception cref="ArgumentException">Thrown if the server already exists in the database.</exception>
        [NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<Server>> AddServerAsync([NotNull] IGuild discordServer)
        {
            if (await IsServerKnownAsync(discordServer))
            {
                return RetrieveEntityResult<Server>.FromError
                (
                    $"A server with the ID {discordServer.Id} has already been added to the database."
                );
            }

            var server = Server.CreateDefault(discordServer);

            await _database.Servers.AddAsync(server);
            await _database.SaveChangesAsync();

            return RetrieveEntityResult<Server>.FromSuccess(server);
        }

        /// <summary>
        /// Gets the description of the server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull]
        public RetrieveEntityResult<string> GetDescription([NotNull] Server server)
        {
            if (server.Description.IsNullOrWhitespace())
            {
                return RetrieveEntityResult<string>.FromError("No description set.");
            }

            return RetrieveEntityResult<string>.FromSuccess(server.Description);
        }

        /// <summary>
        /// Sets the description of the server.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="description">The new description.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetDescriptionAsync
        (
            [NotNull] Server server,
            [NotNull] string description
        )
        {
            if (description.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError
                (
                    "The description must not be empty."
                );
            }

            if (server.Description == description)
            {
                return ModifyEntityResult.FromError
                (
                    "That's already the server's description."
                );
            }

            if (description.Length > 800)
            {
                return ModifyEntityResult.FromError
                (
                    "The description may not be longer than 800 characters."
                );
            }

            server.Description = description;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets the server's join message.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull]
        public RetrieveEntityResult<string> GetJoinMessage([NotNull] Server server)
        {
            if (server.JoinMessage.IsNullOrWhitespace())
            {
                return RetrieveEntityResult<string>.FromError("No join message set.");
            }

            return RetrieveEntityResult<string>.FromSuccess(server.JoinMessage);
        }

        /// <summary>
        /// Sets the server's join message.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="joinMessage">The new join message.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetJoinMessageAsync
        (
            [NotNull] Server server,
            [NotNull] string joinMessage
        )
        {
            if (joinMessage.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError
                (
                    "The join message must not be empty."
                );
            }

            if (server.JoinMessage == joinMessage)
            {
                return ModifyEntityResult.FromError
                (
                    "That's already the server's join message."
                );
            }

            if (joinMessage.Length > 1200)
            {
                return ModifyEntityResult.FromError
                (
                    "The join message may not be longer than 1200 characters."
                );
            }

            server.JoinMessage = joinMessage;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets a value indicating whether the server is NSFW.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="isNsfw">Whether or not the server is NSFW.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetIsNSFWAsync
        (
            [NotNull] Server server,
            bool isNsfw
        )
        {
            if (server.IsNSFW == isNsfw)
            {
                return ModifyEntityResult.FromError
                (
                    $"The server is already {(isNsfw ? string.Empty : "not")} NSFW."
                );
            }

            server.IsNSFW = isNsfw;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets a value indicating whether the server should send first-join messages.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="sendJoinMessage">Whether or not the server should send join messages.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        [NotNull, ItemNotNull]
        public async Task<ModifyEntityResult> SetSendJoinMessageAsync
        (
            [NotNull] Server server,
            bool sendJoinMessage
        )
        {
            if (server.SendJoinMessage == sendJoinMessage)
            {
                return ModifyEntityResult.FromError
                (
                    $"The server already {(sendJoinMessage ? string.Empty : "not")} sending first-join messages."
                );
            }

            server.SendJoinMessage = sendJoinMessage;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
