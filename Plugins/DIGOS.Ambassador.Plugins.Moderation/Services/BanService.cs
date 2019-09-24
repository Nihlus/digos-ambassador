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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Moderation.Services
{
    /// <summary>
    /// Acts as an interface for accessing and modifying bans.
    /// </summary>
    [PublicAPI]
    public sealed class BanService
    {
        [NotNull] private readonly ModerationDatabaseContext _database;
        [NotNull] private readonly ServerService _servers;
        [NotNull] private readonly UserService _users;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanService"/> class.
        /// </summary>
        /// <param name="database">The database context.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="users">The user service.</param>
        public BanService
        (
            [NotNull] ModerationDatabaseContext database,
            [NotNull] ServerService servers,
            [NotNull] UserService users
        )
        {
            _database = database;
            _servers = servers;
            _users = users;
        }

        /// <summary>
        /// Gets the bans attached to the given user.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <returns>The bans.</returns>
        public IQueryable<UserBan> GetBans([NotNull] IGuildUser user)
        {
            return _database.UserBans.Where
            (
                n => n.User.DiscordID == (long)user.Id && n.Server.DiscordID == (long)user.Guild.Id
            );
        }

        /// <summary>
        /// Retrieves a ban with the given ID from the database.
        /// </summary>
        /// <param name="server">The server the ban is on.</param>
        /// <param name="banID">The ban ID.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<UserBan>> GetBanAsync([NotNull] IGuild server, long banID)
        {
            // The server isn't strictly required here, but it prevents leaking bans between servers.
            var ban = await _database.UserBans.FirstOrDefaultAsync
            (
                n => n.ID == banID &&
                     n.Server.DiscordID == (long)server.Id
            );

            if (ban is null)
            {
                return RetrieveEntityResult<UserBan>.FromError("There's no ban with that ID in the database.");
            }

            return ban;
        }

        /// <summary>
        /// Creates a ban for the given user.
        /// </summary>
        /// <param name="authorUser">The author of the ban.</param>
        /// <param name="guildUser">The user.</param>
        /// <param name="reason">The reason of the ban.</param>
        /// <param name="messageID">The message that caused the ban, if any.</param>
        /// <param name="expiresOn">The expiry date for the ban, if any.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<UserBan>> CreateBanAsync
        (
            [NotNull] IUser authorUser,
            [NotNull] IGuildUser guildUser,
            [NotNull] string reason,
            [CanBeNull] long? messageID = null,
            [CanBeNull] DateTime? expiresOn = null
        )
        {
            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<UserBan>.FromError(getServer);
            }

            var server = getServer.Entity;

            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return CreateEntityResult<UserBan>.FromError(getUser);
            }

            var user = getUser.Entity;

            var getAuthor = await _users.GetOrRegisterUserAsync(authorUser);
            if (!getAuthor.IsSuccess)
            {
                return CreateEntityResult<UserBan>.FromError(getAuthor);
            }

            var author = getAuthor.Entity;

            var ban = new UserBan(server, user, author, string.Empty);

            var setReason = await SetBanReasonAsync(ban, reason);
            if (!setReason.IsSuccess)
            {
                return CreateEntityResult<UserBan>.FromError(setReason);
            }

            if (!(messageID is null))
            {
                var setMessage = await SetBanContextMessageAsync(ban, messageID.Value);
                if (!setMessage.IsSuccess)
                {
                    return CreateEntityResult<UserBan>.FromError(setMessage);
                }
            }

            if (!(expiresOn is null))
            {
                var setExpiry = await SetBanExpiryDateAsync(ban, expiresOn.Value);
                if (!setExpiry.IsSuccess)
                {
                    return CreateEntityResult<UserBan>.FromError(setExpiry);
                }
            }

            _database.UserBans.Update(ban);

            await _database.SaveChangesAsync();

            // Requery the database
            var getBan = await GetBanAsync(guildUser.Guild, ban.ID);
            if (!getBan.IsSuccess)
            {
                return CreateEntityResult<UserBan>.FromError(getBan);
            }

            return CreateEntityResult<UserBan>.FromSuccess(getBan.Entity);
        }

        /// <summary>
        /// Sets the reasons of the given ban.
        /// </summary>
        /// <param name="ban">The ban.</param>
        /// <param name="reason">The reason.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetBanReasonAsync([NotNull] UserBan ban, [NotNull] string reason)
        {
            if (reason.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError("You must provide some reason for the ban.");
            }

            if (reason.Length > 1024)
            {
                return ModifyEntityResult.FromError
                (
                    "The ban is too long. It can be at most 1024 characters."
                );
            }

            if (ban.Reason == reason)
            {
                return ModifyEntityResult.FromError("That's already the ban's reason.");
            }

            ban.Reason = reason;
            ban.NotifyUpdate();

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the contextually relevant message for the ban.
        /// </summary>
        /// <param name="ban">The ban.</param>
        /// <param name="messageID">The message.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetBanContextMessageAsync
        (
            [NotNull] UserBan ban,
            long messageID
        )
        {
            if (ban.MessageID == messageID)
            {
                return ModifyEntityResult.FromError("That's already the ban's context message.");
            }

            ban.MessageID = messageID;
            ban.NotifyUpdate();

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the date and time at which the ban expires.
        /// </summary>
        /// <param name="ban">The ban.</param>
        /// <param name="expiresOn">The date and time at which the ban expires.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetBanExpiryDateAsync
        (
            [NotNull] UserBan ban,
            DateTime expiresOn
        )
        {
            if (ban.ExpiresOn == expiresOn)
            {
                return ModifyEntityResult.FromError("That's already the ban's expiry date.");
            }

            if (expiresOn < DateTime.UtcNow)
            {
                return ModifyEntityResult.FromError("Bans can't expire in the past.");
            }

            ban.ExpiresOn = expiresOn;
            ban.NotifyUpdate();

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Deletes the given ban.
        /// </summary>
        /// <param name="ban">The ban to delete.</param>
        /// <returns>A deletion result which may or may ban have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteBanAsync([NotNull] UserBan ban)
        {
            if (!_database.UserBans.Any(n => n.ID == ban.ID))
            {
                return DeleteEntityResult.FromError
                (
                    "That ban isn't in the database. This is probably an error in the bot."
                );
            }

            _database.UserBans.Remove(ban);
            await _database.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }
    }
}
