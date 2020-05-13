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

using System;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using Discord;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
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
        private readonly ServerService _servers;
        private readonly UserService _users;
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleService"/> class.
        /// </summary>
        /// <param name="database">The database.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="serviceProvider">The service provider.</param>
        public AutoroleService
        (
            AutoroleDatabaseContext database,
            ServerService servers,
            UserService users,
            IServiceProvider serviceProvider
        )
        {
            _database = database;
            _servers = servers;
            _users = users;
            _serviceProvider = serviceProvider;
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

            var getServer = await _servers.GetOrRegisterServerAsync(discordRole.Guild);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<AutoroleConfiguration>.FromError(getServer);
            }

            var server = getServer.Entity;

            var autorole = _database.CreateProxy<AutoroleConfiguration>(server, discordRole);
            if (autorole is null)
            {
                return CreateEntityResult<AutoroleConfiguration>.FromError
                (
                    "Failed to create a valid proxy for the autorole."
                );
            }

            _database.Autoroles.Update(autorole);
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

            if (!autorole.Conditions.Any())
            {
                return ModifyEntityResult.FromError("The autorole doesn't have any configured conditions.");
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

            if (autorole.Conditions.Count == 1 && autorole.IsEnabled)
            {
                return ModifyEntityResult.FromError
                (
                    "The autorole is still enabled, so it requires at least one condition to be present. " +
                    "Either disable the role, or add more conditions."
                );
            }

            autorole.Conditions.Remove(condition);
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Adds a condition to the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="condition">The condition.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> AddConditionAsync
        (
            AutoroleConfiguration autorole,
            AutoroleCondition condition
        )
        {
            if (autorole.Conditions.Any(c => c.HasSameConditionsAs(condition)))
            {
                return ModifyEntityResult.FromError
                (
                    "There's already a condition with the same settings in the autorole."
                );
            }

            autorole.Conditions.Add(condition);
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets a condition of the specified ID and type from the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="conditionID">The ID of the condition.</param>
        /// <typeparam name="TCondition">The type of the condition.</typeparam>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public RetrieveEntityResult<TCondition> GetCondition<TCondition>
        (
            AutoroleConfiguration autorole,
            long conditionID
        )
            where TCondition : AutoroleCondition
        {
            var condition = autorole.Conditions.FirstOrDefault(c => c.ID == conditionID);
            if (condition is null)
            {
                return RetrieveEntityResult<TCondition>.FromError
                (
                    "The autorole doesn't have any condition with that ID."
                );
            }

            if (!(condition is TCondition))
            {
                return RetrieveEntityResult<TCondition>.FromError
                (
                    "The condition with that ID isn't this kind of condition."
                );
            }

            return (TCondition)condition;
        }

        /// <summary>
        /// Gets or creates an autorole confirmation for a given user.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="discordUser">The user.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<AutoroleConfirmation>> GetOrCreateAutoroleConfirmationAsync
        (
            AutoroleConfiguration autorole,
            IUser discordUser
        )
        {
            var existingConfirmation = await _database.AutoroleConfirmations.AsQueryable().FirstOrDefaultAsync
            (
                ac => ac.User.DiscordID == (long)discordUser.Id
            );

            if (!(existingConfirmation is null))
            {
                return existingConfirmation;
            }

            var getUser = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<AutoroleConfirmation>.FromError(getUser);
            }

            var user = getUser.Entity;
            var newConfirmation = _database.CreateProxy<AutoroleConfirmation>(autorole, user, false);

            _database.AutoroleConfirmations.Update(newConfirmation);
            await _database.SaveChangesAsync();

            return newConfirmation;
        }

        /// <summary>
        /// Explicitly affirms an autorole assignment.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="discordUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> AffirmAutoroleAsync
        (
            AutoroleConfiguration autorole,
            IUser discordUser
        )
        {
            if (autorole.RequiresConfirmation)
            {
                return ModifyEntityResult.FromError("The autorole doesn't require explicit affirmation.");
            }

            var getCondition = await GetOrCreateAutoroleConfirmationAsync(autorole, discordUser);
            if (!getCondition.IsSuccess)
            {
                return ModifyEntityResult.FromError(getCondition);
            }

            var condition = getCondition.Entity;
            if (condition.IsConfirmed)
            {
                return ModifyEntityResult.FromError("The autorole assignment has already been affirmed.");
            }

            condition.IsConfirmed = true;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Explicitly denies an autorole assignment.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="discordUser">The user.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> DenyAutoroleAsync
        (
            AutoroleConfiguration autorole,
            IUser discordUser
        )
        {
            if (autorole.RequiresConfirmation)
            {
                return ModifyEntityResult.FromError("The autorole doesn't require explicit affirmation.");
            }

            var getCondition = await GetOrCreateAutoroleConfirmationAsync(autorole, discordUser);
            if (!getCondition.IsSuccess)
            {
                return ModifyEntityResult.FromError(getCondition);
            }

            var condition = getCondition.Entity;
            if (condition.IsConfirmed)
            {
                return ModifyEntityResult.FromError
                (
                    "The autorole assignment has already been denied, or has never been affirmed."
                );
            }

            condition.IsConfirmed = false;
            await _database.SaveChangesAsync();

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether a given user is qualified for the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="user">The user.</param>
        /// <returns>true if the user qualifies; otherwise, false.</returns>
        public async Task<bool> IsUserQualifiedForAutoroleAsync(AutoroleConfiguration autorole, IGuildUser user)
        {
            return (await Task.WhenAll
            (
                autorole.Conditions.Select
                (
                    c => c.IsConditionFulfilledForUser(_serviceProvider, user)
                )
            ))
            .All(c => c);
        }

        /// <summary>
        /// Creates a proxy object for the given condition type.
        /// </summary>
        /// <param name="args">The constructor arguments for the condition.</param>
        /// <typeparam name="TCondition">The condition type.</typeparam>
        /// <returns>The condition, or null.</returns>
        public TCondition? CreateConditionProxy<TCondition>(params object[] args)
            where TCondition : AutoroleCondition
        {
            return _database.CreateProxy<TCondition>(args);
        }

        /// <summary>
        /// Explicitly saves the database.
        /// </summary>
        /// <returns>The number of entities saved.</returns>
        public Task<int> SaveChangesAsync()
        {
            return _database.SaveChangesAsync();
        }

        /// <summary>
        /// Gets all autoroles in the database.
        /// </summary>
        /// <param name="guild">The Discord guild.</param>
        /// <returns>The autoroles.</returns>
        public IQueryable<AutoroleConfiguration> GetAutoroles(IGuild guild)
        {
            return _database.Autoroles.AsQueryable().Where(a => a.Server.DiscordID == (long)guild.Id);
        }
    }
}
