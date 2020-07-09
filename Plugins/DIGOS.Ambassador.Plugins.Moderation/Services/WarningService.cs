//
//  WarningService.cs
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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Discord;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying warnings.
    /// </summary>
    public sealed class WarningService
    {
        private readonly ModerationDatabaseContext _database;
        private readonly ServerService _servers;
        private readonly UserService _users;

        /// <summary>
        /// Initializes a new instance of the <see cref="WarningService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="users">The user service.</param>
        public WarningService
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
        /// Gets the warnings issued on the given guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>The warnings.</returns>
        public Task<IReadOnlyList<UserWarning>> GetWarningsAsync(IGuild guild, CancellationToken ct = default)
        {
            return _database.UserWarnings.ServersideQueryAsync
            (
                q => q.Where
                (
                    n => n.Server.DiscordID == (long)guild.Id
                ),
                ct
            );
        }

        /// <summary>
        /// Gets the warnings attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>The warnings.</returns>
        public Task<IReadOnlyList<UserWarning>> GetWarningsAsync(IGuildUser user, CancellationToken ct = default)
        {
            return _database.UserWarnings.ServersideQueryAsync
            (
                q => q.Where
                (
                    n => n.User.DiscordID == (long)user.Id && n.Server.DiscordID == (long)user.Guild.Id
                ),
                ct
            );
        }

        /// <summary>
        /// Retrieves a warning with the given ID from the database.
        /// </summary>
        /// <param name="server">The server the warning is on.</param>
        /// <param name="warningID">The warning ID.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<UserWarning>> GetWarningAsync
        (
            IGuild server,
            long warningID,
            CancellationToken ct = default
        )
        {
            // The server isn't strictly required here, but it prevents leaking warnings between servers.
            var warning = await _database.UserWarnings.ServersideQueryAsync
            (
                q => q.FirstOrDefaultAsync
                (
                    n => n.ID == warningID && n.Server.DiscordID == (long)server.Id,
                    ct
                )
            );

            if (warning is null)
            {
                return RetrieveEntityResult<UserWarning>.FromError("There's no warning with that ID in the database.");
            }

            return warning;
        }

        /// <summary>
        /// Creates a warning for the given user.
        /// </summary>
        /// <param name="authorUser">The author of the warning.</param>
        /// <param name="guildUser">The user.</param>
        /// <param name="reason">The reason of the warning.</param>
        /// <param name="messageID">The message that caused the warning, if any.</param>
        /// <param name="expiresOn">The expiry date for the warning, if any.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<UserWarning>> CreateWarningAsync
        (
            IUser authorUser,
            IGuildUser guildUser,
            string reason,
            long? messageID = null,
            DateTime? expiresOn = null,
            CancellationToken ct = default
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild, ct);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<UserWarning>.FromError(getServer);
            }

            var server = getServer.Entity;

            var getUser = await _users.GetOrRegisterUserAsync(guildUser, ct);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<UserWarning>.FromError(getUser);
            }

            var user = getUser.Entity;

            var getAuthor = await _users.GetOrRegisterUserAsync(authorUser, ct);
            if (!getAuthor.IsSuccess)
            {
                return CreateEntityResult<UserWarning>.FromError(getAuthor);
            }

            var author = getAuthor.Entity;

            var warning = _database.CreateProxy<UserWarning>(server, user, author, string.Empty, null, null);
            _database.UserWarnings.Update(warning);

            var setReason = await SetWarningReasonAsync(warning, reason, ct);
            if (!setReason.IsSuccess)
            {
                return CreateEntityResult<UserWarning>.FromError(setReason);
            }

            if (!(messageID is null))
            {
                var setMessage = await SetWarningContextMessageAsync(warning, messageID.Value, ct);
                if (!setMessage.IsSuccess)
                {
                    return CreateEntityResult<UserWarning>.FromError(setMessage);
                }
            }

            if (!(expiresOn is null))
            {
                var setExpiry = await SetWarningExpiryDateAsync(warning, expiresOn.Value, ct);
                if (!setExpiry.IsSuccess)
                {
                    return CreateEntityResult<UserWarning>.FromError(setExpiry);
                }
            }

            await _database.SaveChangesAsync(ct);

            return warning;
        }

        /// <summary>
        /// Sets the reasons of the given warning.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <param name="reason">The reason.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetWarningReasonAsync
        (
            UserWarning warning,
            string reason,
            CancellationToken ct = default
        )
        {
            if (reason.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError("You must provide some reason for the warning.");
            }

            if (reason.Length > 1024)
            {
                return ModifyEntityResult.FromError
                (
                    "The warning is too long. It can be at most 1024 characters."
                );
            }

            if (warning.Reason == reason)
            {
                return ModifyEntityResult.FromError("That's already the warning's reason.");
            }

            warning.Reason = reason;
            warning.NotifyUpdate();

            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the contextually relevant message for the warning.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <param name="messageID">The message.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetWarningContextMessageAsync
        (
            UserWarning warning,
            long messageID,
            CancellationToken ct = default
        )
        {
            if (warning.MessageID == messageID)
            {
                return ModifyEntityResult.FromError("That's already the warning's context message.");
            }

            warning.MessageID = messageID;
            warning.NotifyUpdate();

            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the date and time at which the warning expires.
        /// </summary>
        /// <param name="warning">The warning.</param>
        /// <param name="expiresOn">The date and time at which the warning expires.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetWarningExpiryDateAsync
        (
            UserWarning warning,
            DateTime expiresOn,
            CancellationToken ct = default
        )
        {
            if (warning.ExpiresOn == expiresOn)
            {
                return ModifyEntityResult.FromError("That's already the warning's expiry date.");
            }

            if (expiresOn < DateTime.UtcNow)
            {
                return ModifyEntityResult.FromError("Warnings can't expire in the past.");
            }

            warning.ExpiresOn = expiresOn;
            warning.NotifyUpdate();

            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Deletes the given warning.
        /// </summary>
        /// <param name="warning">The warning to delete.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may warning have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteWarningAsync
        (
            UserWarning warning,
            CancellationToken ct = default
        )
        {
            if (!_database.UserWarnings.Any(n => n.ID == warning.ID))
            {
                return DeleteEntityResult.FromError
                (
                    "That warning isn't in the database. This is probably an error in the bot."
                );
            }

            _database.UserWarnings.Remove(warning);
            await _database.SaveChangesAsync(ct);

            return DeleteEntityResult.FromSuccess();
        }
    }
}
