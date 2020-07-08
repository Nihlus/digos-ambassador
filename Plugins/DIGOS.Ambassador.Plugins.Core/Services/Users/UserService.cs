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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Core.Services.Users
{
    /// <summary>
    /// Handles user-related logic.
    /// </summary>
    [PublicAPI]
    public sealed class UserService
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
        [Pure]
        public async Task<bool> IsUserKnownAsync(IUser discordUser)
        {
            var hasUser = await _database.Users.ServersideQueryAsync
            (
                q => q
                    .Where(u => u.DiscordID == (long)discordUser.Id)
                    .AnyAsync()
            );

            return hasUser;
        }

        /// <summary>
        /// Gets an existing set of information about a Discord user, or registers them with the database if one is not found.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <returns>Stored information about the user.</returns>
        public async Task<RetrieveEntityResult<User>> GetOrRegisterUserAsync
        (
            IUser discordUser
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
        [Pure]
        public async Task<RetrieveEntityResult<User>> GetUserAsync(IUser discordUser)
        {
            var user = await _database.Users.ServersideQueryAsync
            (
                q => q
                    .Where(u => u.DiscordID == (long)discordUser.Id)
                    .SingleOrDefaultAsync()
            );

            if (!(user is null))
            {
                return user;
            }

            return RetrieveEntityResult<User>.FromError("Unknown user.");
        }

        /// <summary>
        /// Adds a Discord user to the database.
        /// </summary>
        /// <param name="discordUser">The Discord user.</param>
        /// <returns>The freshly created information about the user.</returns>
        /// <exception cref="ArgumentException">Thrown if the user already exists in the database.</exception>
        public async Task<RetrieveEntityResult<User>> AddUserAsync(IUser discordUser)
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

            var newUser = _database.CreateProxy<User>((long)discordUser.Id);

            _database.Users.Update(newUser);
            await _database.SaveChangesAsync();

            return newUser;
        }

        /// <summary>
        /// Sets the user's timezone.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="timezoneOffset">The timezone.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ModifyEntityResult> SetUserTimezoneAsync(User user, int timezoneOffset)
        {
            if (timezoneOffset < -12 || timezoneOffset > 14)
            {
                return ModifyEntityResult.FromError($"{timezoneOffset} is not a valid offset.");
            }

            if (user.Timezone == timezoneOffset)
            {
                return ModifyEntityResult.FromError("That's already your timezone.'");
            }

            user.Timezone = timezoneOffset;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the user's bio.
        /// </summary>
        /// <param name="user">The user.</param>
        /// <param name="bio">The bio.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<ModifyEntityResult> SetUserBioAsync(User user, string bio)
        {
            if (bio.IsNullOrWhitespace())
            {
                return ModifyEntityResult.FromError("You must provide a bio.");
            }

            if (bio.Length > 1024)
            {
                return ModifyEntityResult.FromError("Your bio may not be longer than 1024 characters.");
            }

            if (user.Bio == bio)
            {
                return ModifyEntityResult.FromError("That's already your bio.");
            }

            user.Bio = bio;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
