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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Discord;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Discord;
using Microsoft.EntityFrameworkCore;
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
        private readonly DiscordService _discord;
        private readonly CharacterService _characters;
        private readonly IDiscordClient _client;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterRoleService"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="discord">The Discord service.</param>
        /// <param name="characters">The character service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="client">The discord client.</param>
        public CharacterRoleService
        (
            CharactersDatabaseContext database,
            ServerService servers,
            DiscordService discord,
            CharacterService characters,
            UserService users,
            IDiscordClient client
        )
        {
            _database = database;
            _servers = servers;
            _discord = discord;
            _characters = characters;
            _users = users;
            _client = client;
        }

        /// <summary>
        /// Creates a new character role from the given Discord role and access condition.
        /// </summary>
        /// <param name="role">The discord role.</param>
        /// <param name="access">The access conditions.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<CharacterRole>> CreateCharacterRoleAsync(IRole role, RoleAccess access)
        {
            var getExistingRoleResult = await GetCharacterRoleAsync(role);
            if (getExistingRoleResult.IsSuccess)
            {
                return CreateEntityResult<CharacterRole>.FromError
                (
                    "That role is already registered as a character role."
                );
            }

            var getServerResult = await _servers.GetOrRegisterServerAsync(role.Guild);
            if (!getServerResult.IsSuccess)
            {
                return CreateEntityResult<CharacterRole>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var characterRole = _database.CreateProxy<CharacterRole>(server, (long)role.Id, access);
            _database.CharacterRoles.Update(characterRole);

            return characterRole;
        }

        /// <summary>
        /// Deletes the character role for the given Discord role.
        /// </summary>
        /// <param name="role">The character role.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteCharacterRoleAsync(CharacterRole role)
        {
            var currentCharactersWithRole = await _characters.GetCharactersAsync
            (
                role.Server,
                q => q
                    .Where(c => c.Role == role)
                    .Where(c => c.IsCurrent)
            );

            _database.CharacterRoles.Remove(role);

            var guild = await _client.GetGuildAsync((ulong)role.Server.DiscordID);
            if (guild is null)
            {
                return DeleteEntityResult.FromError("Could not retrieve the guild the role was on.");
            }

            foreach (var character in currentCharactersWithRole)
            {
                var owner = await guild.GetUserAsync((ulong)character.Owner.DiscordID);
                var discordRole = guild.GetRole((ulong)role.DiscordID);

                if (owner is null || discordRole is null)
                {
                    return DeleteEntityResult.FromError("Failed to get the owner or role.");
                }

                var removeRole = await _discord.RemoveUserRoleAsync(owner, discordRole);
                if (!removeRole.IsSuccess)
                {
                    return DeleteEntityResult.FromError(removeRole);
                }
            }
            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets an existing character role from the database.
        /// </summary>
        /// <param name="role">The discord role.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<CharacterRole>> GetCharacterRoleAsync(IRole role)
        {
            var characterRoles = await _database.CharacterRoles.UnifiedQueryAsync
            (
                q => q.Where(r => r.Server.DiscordID == (long)role.Guild.Id && r.DiscordID == (long)role.Id)
            );

            var characterRole = characterRoles.SingleOrDefault();

            if (!(characterRole is null))
            {
                return characterRole;
            }

            return RetrieveEntityResult<CharacterRole>.FromError
            (
                "That role is not registered as a character role."
            );
        }

        /// <summary>
        /// Gets the roles available on the given server.
        /// </summary>
        /// <param name="guild">The Discord guild.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IQueryable<CharacterRole>>> GetCharacterRolesAsync(IGuild guild)
        {
            var getServerResult = await _servers.GetOrRegisterServerAsync(guild);
            if (!getServerResult.IsSuccess)
            {
                return RetrieveEntityResult<IQueryable<CharacterRole>>.FromError(getServerResult);
            }

            var server = getServerResult.Entity;

            var roles = _database.CharacterRoles.AsQueryable().Where(r => r.Server == server);

            return RetrieveEntityResult<IQueryable<CharacterRole>>.FromSuccess(roles);
        }

        /// <summary>
        /// Sets the access conditions for the given character role.
        /// </summary>
        /// <param name="role">The character role.</param>
        /// <param name="access">The access conditions.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterRoleAccessAsync(CharacterRole role, RoleAccess access)
        {
            if (role.Access == access)
            {
                return ModifyEntityResult.FromError("The role already has those access conditions.");
            }

            role.Access = access;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the custom role of a character.
        /// </summary>
        /// <param name="guildUser">The owner of the character.</param>
        /// <param name="character">The character.</param>
        /// <param name="characterRole">The role to set.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterRoleAsync(IGuildUser guildUser, Character character, CharacterRole characterRole)
        {
            if (character.Role == characterRole)
            {
                return ModifyEntityResult.FromError("The character already has that role.");
            }

            if (character.IsCurrent)
            {
                if (!(character.Role is null))
                {
                    var oldRole = guildUser.Guild.GetRole((ulong)character.Role.DiscordID);
                    if (!(oldRole is null))
                    {
                        var removeRole = await _discord.RemoveUserRoleAsync(guildUser, oldRole);
                        if (!removeRole.IsSuccess)
                        {
                            return removeRole;
                        }
                    }
                }

                var newRole = guildUser.Guild.GetRole((ulong)characterRole.DiscordID);
                if (newRole is null)
                {
                    return ModifyEntityResult.FromError("Failed to get the new role.");
                }

                var addRole = await _discord.AddUserRoleAsync(guildUser, newRole);
                if (!addRole.IsSuccess)
                {
                    return addRole;
                }
            }

            character.Role = characterRole;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears the custom role of a character.
        /// </summary>
        /// <param name="owner">The character's owner.</param>
        /// <param name="character">The character.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearCharacterRoleAsync(IGuildUser owner, Character character)
        {
            if (character.Role is null)
            {
                return ModifyEntityResult.FromError("The character doesn't have a role set.");
            }

            var role = owner.Guild.GetRole((ulong)character.Role.DiscordID);
            if (role is null)
            {
                return ModifyEntityResult.FromError("Could not get the role from Discord.");
            }

            var removeRole = await _discord.RemoveUserRoleAsync(owner, role);
            if (!removeRole.IsSuccess)
            {
                return removeRole;
            }

            character.Role = null;

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Updates the roles of the given user, removing old roles and applying new ones.
        /// </summary>
        /// <param name="guildUser">The user.</param>
        /// <param name="previousCharacter">The character they previously were.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> UpdateUserRolesAsync
        (
            IGuildUser guildUser,
            Character? previousCharacter = null
        )
        {
            var getUser = await _users.GetOrRegisterUserAsync(guildUser);
            if (!getUser.IsSuccess)
            {
                return ModifyEntityResult.FromError(getUser);
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guildUser.Guild);
            if (!getServer.IsSuccess)
            {
                return ModifyEntityResult.FromError(getServer);
            }

            var user = getUser.Entity;
            var server = getServer.Entity;

            var getNewCharacter = await _characters.GetCurrentCharacterAsync(user, server);
            if (!getNewCharacter.IsSuccess)
            {
                // Clear any old role
                if (previousCharacter?.Role is null)
                {
                    return ModifyEntityResult.FromSuccess();
                }

                var oldRole = guildUser.Guild.GetRole((ulong)previousCharacter.Role.DiscordID);
                if (oldRole is null)
                {
                    return ModifyEntityResult.FromSuccess();
                }

                var removeRole = await _discord.RemoveUserRoleAsync(guildUser, oldRole);
                if (!removeRole.IsSuccess)
                {
                    return removeRole;
                }

                return ModifyEntityResult.FromSuccess();
            }

            var newCharacter = getNewCharacter.Entity;

            // First, quick sanity check - do we need to remove the role?
            if (!(previousCharacter?.Role is null) && newCharacter.Role == previousCharacter.Role)
            {
                return ModifyEntityResult.FromSuccess();
            }

            // Clear any old role
            if (!(previousCharacter?.Role is null))
            {
                var oldRole = guildUser.Guild.GetRole((ulong)previousCharacter.Role.DiscordID);
                if (!(oldRole is null))
                {
                    var removeRole = await _discord.RemoveUserRoleAsync(guildUser, oldRole);
                    if (!removeRole.IsSuccess)
                    {
                        return removeRole;
                    }
                }
            }

            if (newCharacter.Role is null)
            {
                return ModifyEntityResult.FromSuccess();
            }

            // Apply any new role
            var newRole = guildUser.Guild.GetRole((ulong)newCharacter.Role.DiscordID);
            if (newRole is null)
            {
                return ModifyEntityResult.FromSuccess();
            }

            var addRole = await _discord.AddUserRoleAsync(guildUser, newRole);
            if (!addRole.IsSuccess)
            {
                return addRole;
            }

            return ModifyEntityResult.FromSuccess();
        }
    }
}
