//
//  UserService.cs
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
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;

namespace DIGOS.Ambassador.Plugins.Core.Services.Users
{
    /// <summary>
    /// Handles user-related logic.
    /// </summary>
    public class UserService
    {
        private readonly CoreDatabaseContext _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserService"/> class.
        /// </summary>
        /// <param name="database">The core database.</param>
        public UserService(CoreDatabaseContext database)
        {
            _database = database;
        }

        /// <summary>
        /// Determines whether or not a Discord user is stored in the database.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <returns><value>true</value> if the user is stored; otherwise, <value>false</value>.</returns>
        [Pure, NotNull]
        public async Task<bool> IsUserKnownAsync([NotNull] IUser discordUser)
        {
            return await _database.Users.AnyAsync(u => u.DiscordID == (long)discordUser.Id);
        }

        /// <summary>
        /// Gets an existing set of information about a Discord user, or registers them with the database if one is not found.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <returns>Stored information about the user.</returns>
        [NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<User>> GetOrRegisterUserAsync
        ([NotNull] IUser discordUser
        )
        {
            if (!await IsUserKnownAsync(discordUser))
            {
                return await AddUserAsync(discordUser);
            }

            return await GetUserAsync(discordUser);
        }

        /// <summary>
        /// Gets a stored user from the database that matches the given Discord user.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <returns>Stored information about the user.</returns>
        [Pure, ItemNotNull]
        public async Task<RetrieveEntityResult<User>> GetUserAsync([NotNull] IUser discordUser)
        {
            var user = await _database.Users.FirstOrDefaultAsync
            (
                u =>
                    u.DiscordID == (long)discordUser.Id
            );

            if (user is null)
            {
                return RetrieveEntityResult<User>.FromError("Unknown user.");
            }

            return RetrieveEntityResult<User>.FromSuccess(user);
        }

        /// <summary>
        /// Adds a Discord user to the database.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <returns>The freshly created information about the user.</returns>
        /// <exception cref="ArgumentException">Thrown if the user already exists in the database.</exception>
        [NotNull, ItemNotNull]
        public async Task<RetrieveEntityResult<User>> AddUserAsync([NotNull] IUser discordUser)
        {
            if (discordUser.IsBot || discordUser.IsWebhook)
            {
                return RetrieveEntityResult<User>.FromError
                (
                    "Users cannot be viewed or created for bots or webhooks."
                );
            }

            if (await IsUserKnownAsync(discordUser))
            {
                return RetrieveEntityResult<User>.FromError
                (
                    $"A user with the ID {discordUser.Id} has already been added to the database."
                );
            }

            var newUser = new User
            {
                DiscordID = (long)discordUser.Id,
                Bio = null,
                Timezone = null
            };

            _database.Users.Update(newUser);

            await _database.SaveChangesAsync();

            // Requery the database
            return await GetUserAsync(discordUser);
        }
    }
}