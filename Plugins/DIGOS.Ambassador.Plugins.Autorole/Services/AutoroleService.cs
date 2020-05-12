//
//  AutoroleService.cs
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
using DIGOS.Ambassador.Plugins.Autorole.Model;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Services
{
    /// <summary>
    /// Handles business logic for autoroles.
    /// </summary>
    [PublicAPI]
    public class AutoroleService
    {
        private readonly AutoroleDatabaseContext _database;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleService"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        public AutoroleService(AutoroleDatabaseContext database)
        {
            _database = database;
        }

        /// <summary>
        /// Determines whether the given discord role has an autorole configuration.
        /// </summary>
        /// <param name="discordRole">The discord role.</param>
        /// <returns>true if the given role has an autorole; otherwise, false.</returns>
        public ValueTask<bool> HasAutoroleAsync(IRole discordRole)
        {
            return _database.Autoroles.AsAsyncEnumerable().AnyAsync(ar => ar.DiscordRoleID == (long)discordRole.Id);
        }

        /// <summary>
        /// Retrieves an autorole configuration from the database.
        /// </summary>
        /// <param name="discordRole">The discord role to get the matching autorole for.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<AutoroleConfiguration>> GetAutoroleAsync(IRole discordRole)
        {
            var autorole = await _database.Autoroles.AsAsyncEnumerable().FirstOrDefaultAsync
            (
                ar => ar.DiscordRoleID == (long)discordRole.Id
            );

            if (autorole is null)
            {
                return RetrieveEntityResult<AutoroleConfiguration>.FromError
                (
                    "No existing autorole configuration could be found."
                );
            }

            return autorole;
        }

        /// <summary>
        /// Creates a new autorole configuration.
        /// </summary>
        /// <param name="discordRole">The role to create the configuration for.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<AutoroleConfiguration>> CreateAutoroleAsync(IRole discordRole)
        {
            if (await HasAutoroleAsync(discordRole))
            {
                return CreateEntityResult<AutoroleConfiguration>.FromError
                (
                    "That role already has an autorole configuration."
                );
            }

            var proxy = _database.CreateProxy(typeof(AutoroleConfiguration), discordRole);
            if (!(proxy is AutoroleConfiguration autorole))
            {
                return CreateEntityResult<AutoroleConfiguration>.FromError
                (
                    "Failed to create a valid proxy for the autorole."
                );
            }

            await _database.Autoroles.AddAsync(autorole);
            await _database.SaveChangesAsync();

            return autorole;
        }

        /// <summary>
        /// Deletes an autorole configuration from the database.
        /// </summary>
        /// <param name="autorole">The role to delete the configuration for.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteAutoroleAsync(AutoroleConfiguration autorole)
        {
            _database.Autoroles.Remove(autorole);
            await _database.SaveChangesAsync();

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Disables the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> EnableAutoroleAsync(AutoroleConfiguration autorole)
        {
            if (autorole.IsEnabled)
            {
                return ModifyEntityResult.FromError("The autorole is already enabled.");
            }

            autorole.IsEnabled = true;

            await _database.SaveChangesAsync();
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Disables the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> DisableAutoroleAsync(AutoroleConfiguration autorole)
        {
            if (!autorole.IsEnabled)
            {
                return ModifyEntityResult.FromError("The autorole is already disabled.");
            }

            autorole.IsEnabled = false;

            await _database.SaveChangesAsync();
            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Removes a condition with the given ID from the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="conditionID">The ID of the condition.</param>
        /// <returns>A modification which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> RemoveConditionAsync(AutoroleConfiguration autorole, long conditionID)
        {
            var condition = autorole.Conditions.FirstOrDefault(c => c.ID == conditionID);
            if (condition is null)
            {
                return ModifyEntityResult.FromError("The autorole doesn't have any condition with that ID.");
            }

            autorole.Conditions.Remove(condition);
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }
    }
}
