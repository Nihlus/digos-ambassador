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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
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
        private readonly ServerService _servers;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterRoleService"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="servers">The server service.</param>
        public CharacterRoleService(CharactersDatabaseContext database, ServerService servers)
        {
            _database = database;
            _servers = servers;
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
            await _database.SaveChangesAsync();

            return characterRole;
        }

        /// <summary>
        /// Deletes the character role for the given Discord role.
        /// </summary>
        /// <param name="role">The character role.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteCharacterRoleAsync(CharacterRole role)
        {
            _database.CharacterRoles.Remove(role);
            await _database.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets an existing character role from the database.
        /// </summary>
        /// <param name="role">The discord role.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<CharacterRole>> GetCharacterRoleAsync(IRole role)
        {
            var characterRole = await _database.CharacterRoles.AsQueryable().FirstOrDefaultAsync
            (
                r => r.Server.DiscordID == (long)role.Guild.Id && r.DiscordID == (long)role.Id
            );

            if (characterRole is null)
            {
                return RetrieveEntityResult<CharacterRole>.FromError
                (
                    "That role is not registered as a character role."
                );
            }

            return characterRole;
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
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets the custom role of a character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="characterRole">The role to set.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetCharacterRoleAsync(Character character, CharacterRole characterRole)
        {
            if (character.Role == characterRole)
            {
                return ModifyEntityResult.FromError("The character already has that role.");
            }

            character.Role = characterRole;

            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears the custom role of a character.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearCharacterRoleAsync(Character character)
        {
            if (character.Role is null)
            {
                return ModifyEntityResult.FromError("The character doesn't have a role set.");
            }

            character.Role = null;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
