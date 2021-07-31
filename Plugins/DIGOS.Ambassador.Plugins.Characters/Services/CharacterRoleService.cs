//
//  CharacterRoleService.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Microsoft.EntityFrameworkCore;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Abstractions.Results;
using Remora.Discord.Core;
using Remora.Discord.Rest.Results;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Characters.Services
{
    /// <summary>
    /// Contains business logic for linking Discord roles to characters.
    /// </summary>
    public class CharacterRoleService
    {
        private readonly CharactersDatabaseContext _database;
        private readonly UserService _users;
        private readonly ServerService _servers;
        private readonly ICharacterService _characters;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterRoleService"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="characters">The character service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="guildAPI">The guild API.</param>
        public CharacterRoleService
        (
            CharactersDatabaseContext database,
            ServerService servers,
            ICharacterService characters,
            UserService users,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _database = database;
            _servers = servers;
            _characters = characters;
            _users = users;
            _guildAPI = guildAPI;
        }

        /// <summary>
        /// Creates a new character role from the given Discord role and access condition.
        /// </summary>
        /// <param name="guildID">The ID of the guild the role is on.</param>
        /// <param name="roleID">The ID of the discord role.</param>
        /// <param name="access">The access conditions.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<Result<CharacterRole>> CreateCharacterRoleAsync
        (
            Snowflake guildID,
            Snowflake roleID,
            RoleAccess access,
            CancellationToken ct = default
        )
        {
            var getExistingRoleResult = await GetCharacterRoleAsync(guildID, roleID, ct);
            if (getExistingRoleResult.IsSuccess)
            {
                return new UserError
                (
                    "That role is already registered as a character role."
                );
            }

            var getServerResult = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServerResult.IsSuccess)
            {
                return Result<CharacterRole>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var characterRole = _database.CreateProxy<CharacterRole>(server, roleID, access);

            _database.CharacterRoles.Update(characterRole);
            await _database.SaveChangesAsync(ct);

            return characterRole;
        }

        /// <summary>
        /// Deletes the character role for the given Discord role.
        /// </summary>
        /// <param name="role">The character role.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<Result> DeleteCharacterRoleAsync
        (
            CharacterRole role,
            CancellationToken ct = default
        )
        {
            var currentOwnersWithRole = await _database.Characters.ServersideQueryAsync
            (
                q => q
                    .Where(c => c.Server == role.Server)
                    .Where(c => c.Role == role)
                    .Where(c => c.IsCurrent)
                    .Select(c => c.Owner)
                    .Distinct(),
                ct
            );

            _database.CharacterRoles.Remove(role);

            foreach (var characterOwner in currentOwnersWithRole)
            {
                var removeRole = await _guildAPI.RemoveGuildMemberRoleAsync
                (
                    role.Server.DiscordID,
                    characterOwner.DiscordID,
                    role.DiscordID,
                    ct: ct
                );

                if (!removeRole.IsSuccess)
                {
                    return removeRole;
                }
            }

            await _database.SaveChangesAsync(ct);
            return Result.FromSuccess();
        }

        /// <summary>
        /// Gets an existing character role from the database.
        /// </summary>
        /// <param name="guildID">The ID of the guild the role is on.</param>
        /// <param name="roleID">The ID of the discord role.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<CharacterRole>> GetCharacterRoleAsync
        (
            Snowflake guildID,
            Snowflake roleID,
            CancellationToken ct = default
        )
        {
            var characterRole = await _database.CharacterRoles.ServersideQueryAsync
            (
                q => q
                    .Where(r => r.Server.DiscordID == guildID && r.DiscordID == roleID)
                    .SingleOrDefaultAsync(ct)
            );

            if (characterRole is not null)
            {
                return characterRole;
            }

            return new UserError
            (
                "That role is not registered as a character role."
            );
        }

        /// <summary>
        /// Gets the roles available on the given server.
        /// </summary>
        /// <param name="guildID">The Discord guild.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<Result<IReadOnlyList<CharacterRole>>> GetCharacterRolesAsync
        (
            Snowflake guildID,
            CancellationToken ct = default
        )
        {
            var roles = await _database.CharacterRoles.ServersideQueryAsync
            (
                q => q.Where(c => c.Server.DiscordID == guildID),
                ct
            );

            return Result<IReadOnlyList<CharacterRole>>.FromSuccess(roles);
        }

        /// <summary>
        /// Sets the access conditions for the given character role.
        /// </summary>
        /// <param name="role">The character role.</param>
        /// <param name="access">The access conditions.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetCharacterRoleAccessAsync
        (
            CharacterRole role,
            RoleAccess access,
            CancellationToken ct = default
        )
        {
            if (role.Access == access)
            {
                return new UserError("The role already has those access conditions.");
            }

            role.Access = access;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Sets the custom role of a character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="character">The character.</param>
        /// <param name="characterRole">The role to set.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> SetCharacterRoleAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character character,
            CharacterRole characterRole,
            CancellationToken ct = default
        )
        {
            if (character.Role == characterRole)
            {
                return new UserError("The character already has that role.");
            }

            if (character.IsCurrent)
            {
                if (character.Role is not null)
                {
                    var removeRole = await _guildAPI.RemoveGuildMemberRoleAsync
                    (
                        guildID,
                        userID,
                        character.Role.DiscordID,
                        ct: ct
                    );

                    if (!removeRole.IsSuccess)
                    {
                        if (removeRole.Error is not DiscordRestResultError rre)
                        {
                            return removeRole;
                        }

                        if (rre.DiscordError.Code is not DiscordError.UnknownRole)
                        {
                            return removeRole;
                        }

                        // It's probably already removed; that's fine
                    }
                }

                var addRole = await _guildAPI.AddGuildMemberRoleAsync(guildID, userID, characterRole.DiscordID, ct: ct);
                if (!addRole.IsSuccess)
                {
                    return addRole;
                }
            }

            character.Role = characterRole;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Clears the custom role of a character.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="character">The character.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> ClearCharacterRoleAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character character,
            CancellationToken ct = default
        )
        {
            if (character.Role is null)
            {
                return new UserError("The character doesn't have a role set.");
            }

            var removeRole = await _guildAPI.RemoveGuildMemberRoleAsync
            (
                guildID,
                userID,
                character.Role.DiscordID,
                ct: ct
            );

            if (!removeRole.IsSuccess)
            {
                if (removeRole.Error is not DiscordRestResultError rre)
                {
                    return removeRole;
                }

                if (rre.DiscordError.Code is not DiscordError.UnknownRole)
                {
                    return removeRole;
                }

                // It's probably already removed; that's fine
            }

            character.Role = null;
            await _database.SaveChangesAsync(ct);

            return Result.FromSuccess();
        }

        /// <summary>
        /// Updates the roles of the given user, removing old roles and applying new ones.
        /// </summary>
        /// <param name="guildID">The ID of the guild the user is on.</param>
        /// <param name="userID">The ID of the discord user.</param>
        /// <param name="previousCharacter">The character they previously were.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<Result> UpdateUserRolesAsync
        (
            Snowflake guildID,
            Snowflake userID,
            Character? previousCharacter = null,
            CancellationToken ct = default
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(userID, ct);
            if (!getUser.IsSuccess)
            {
                return Result.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildID, ct);
            if (!getServer.IsSuccess)
            {
                return Result.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            var getNewCharacter = await _characters.GetCurrentCharacterAsync(user, server, ct);
            if (!getNewCharacter.IsSuccess)
            {
                // Clear any old role
                if (previousCharacter?.Role is null)
                {
                    return Result.FromSuccess();
                }

                var removeRole = await _guildAPI.RemoveGuildMemberRoleAsync
                (
                    guildID,
                    userID,
                    previousCharacter.Role.DiscordID,
                    ct: ct
                );

                if (removeRole.IsSuccess)
                {
                    return Result.FromSuccess();
                }

                if (removeRole.Error is not DiscordRestResultError rre)
                {
                    return removeRole;
                }

                // It's probably already removed; that's fine
                return rre.DiscordError.Code is not DiscordError.UnknownRole
                    ? removeRole
                    : Result.FromSuccess();
            }

            var newCharacter = getNewCharacter.Entity;

            // First, quick sanity check - do we need to remove the role?
            if (previousCharacter?.Role is not null && newCharacter.Role == previousCharacter.Role)
            {
                return Result.FromSuccess();
            }

            // Clear any old role
            if (previousCharacter?.Role is not null)
            {
                var removeRole = await _guildAPI.RemoveGuildMemberRoleAsync
                (
                    guildID,
                    userID,
                    previousCharacter.Role.DiscordID,
                    ct: ct
                );

                if (!removeRole.IsSuccess)
                {
                    if (removeRole.Error is not DiscordRestResultError rre)
                    {
                        return removeRole;
                    }

                    if (rre.DiscordError.Code is not DiscordError.UnknownRole)
                    {
                        return removeRole;
                    }

                    // It's probably already removed; that's fine
                }
            }

            if (newCharacter.Role is null)
            {
                return Result.FromSuccess();
            }

            // Apply any new role
            return await _guildAPI.AddGuildMemberRoleAsync(guildID, userID, newCharacter.Role.DiscordID, ct: ct);
        }
    }
}
