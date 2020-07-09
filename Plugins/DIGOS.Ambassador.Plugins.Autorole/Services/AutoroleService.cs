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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
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
    public sealed class AutoroleService
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
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>true if the given role has an autorole; otherwise, false.</returns>
        public async ValueTask<bool> HasAutoroleAsync
        (
            IRole discordRole,
            CancellationToken ct = default
        )
        {
            return await _database.Autoroles.ServersideQueryAsync
            (
                q => q
                    .Where(ar => ar.DiscordRoleID == (long)discordRole.Id)
                    .AnyAsync(ct)
            );
        }

        /// <summary>
        /// Retrieves an autorole configuration from the database.
        /// </summary>
        /// <param name="discordRole">The discord role to get the matching autorole for.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<AutoroleConfiguration>> GetAutoroleAsync
        (
            IRole discordRole,
            CancellationToken ct = default
        )
        {
            var autorole = await _database.Autoroles.ServersideQueryAsync
            (
                q => q
                    .Where(ar => ar.DiscordRoleID == (long)discordRole.Id)
                    .SingleOrDefaultAsync(ct)
            );

            if (!(autorole is null))
            {
                return autorole;
            }

            return RetrieveEntityResult<AutoroleConfiguration>.FromError
            (
                "No existing autorole configuration could be found."
            );
        }

        /// <summary>
        /// Creates a new autorole configuration.
        /// </summary>
        /// <param name="discordRole">The role to create the configuration for.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A creation result which may or may not have succeeded.</returns>
        public async Task<CreateEntityResult<AutoroleConfiguration>> CreateAutoroleAsync
        (
            IRole discordRole,
            CancellationToken ct = default
        )
        {
            if (await HasAutoroleAsync(discordRole, ct))
            {
                return CreateEntityResult<AutoroleConfiguration>.FromError
                (
                    "That role already has an autorole configuration."
                );
            }

            var getServer = await _servers.GetOrRegisterServerAsync(discordRole.Guild, ct);
            if (!getServer.IsSuccess)
            {
                return CreateEntityResult<AutoroleConfiguration>.FromError(getServer);
            }

            var server = getServer.Entity;

            var autorole = _database.CreateProxy<AutoroleConfiguration>(server, discordRole);

            _database.Autoroles.Update(autorole);
            await _database.SaveChangesAsync(ct);

            return autorole;
        }

        /// <summary>
        /// Deletes an autorole configuration from the database.
        /// </summary>
        /// <param name="autorole">The role to delete the configuration for.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> DeleteAutoroleAsync
        (
            AutoroleConfiguration autorole,
            CancellationToken ct = default
        )
        {
            _database.Autoroles.Remove(autorole);
            await _database.SaveChangesAsync(ct);

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Disables the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> EnableAutoroleAsync
        (
            AutoroleConfiguration autorole,
            CancellationToken ct = default
        )
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Disables the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> DisableAutoroleAsync
        (
            AutoroleConfiguration autorole,
            CancellationToken ct = default
        )
        {
            if (!autorole.IsEnabled)
            {
                return ModifyEntityResult.FromError("The autorole is already disabled.");
            }

            autorole.IsEnabled = false;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Removes a condition with the given ID from the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="conditionID">The ID of the condition.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> RemoveConditionAsync
        (
            AutoroleConfiguration autorole,
            long conditionID,
            CancellationToken ct = default
        )
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Adds a condition to the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="condition">The condition.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> AddConditionAsync
        (
            AutoroleConfiguration autorole,
            AutoroleCondition condition,
            CancellationToken ct = default
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
            await _database.SaveChangesAsync(ct);

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
        /// Removes an autorole confirmation entry from the database.
        /// </summary>
        /// <param name="confirmation">The confirmation.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A deletion result which may or may not have succeeded.</returns>
        public async Task<DeleteEntityResult> RemoveAutoroleConfirmationAsync
        (
            AutoroleConfirmation confirmation,
            CancellationToken ct = default
        )
        {
            _database.AutoroleConfirmations.Remove(confirmation);
            await _database.SaveChangesAsync(ct);

            return DeleteEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets or creates an autorole confirmation for a given user.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<AutoroleConfirmation>> GetOrCreateAutoroleConfirmationAsync
        (
            AutoroleConfiguration autorole,
            IUser discordUser,
            CancellationToken ct = default
        )
        {
            if (!autorole.RequiresConfirmation)
            {
                return RetrieveEntityResult<AutoroleConfirmation>.FromError
                (
                    "The autorole does not require confirmation."
                );
            }

            var confirmation = await _database.AutoroleConfirmations.ServersideQueryAsync
            (
                q => q
                    .Where(ac => ac.Autorole == autorole)
                    .Where(ac => ac.User.DiscordID == (long)discordUser.Id)
                    .SingleOrDefaultAsync(ct)
            );

            if (!(confirmation is null))
            {
                return confirmation;
            }

            var getUser = await _users.GetOrRegisterUserAsync(discordUser, ct);
            if (!getUser.IsSuccess)
            {
                return RetrieveEntityResult<AutoroleConfirmation>.FromError(getUser);
            }

            var user = getUser.Entity;

            var newConfirmation = _database.CreateProxy<AutoroleConfirmation>(autorole, user, false);

            _database.AutoroleConfirmations.Update(newConfirmation);
            await _database.SaveChangesAsync(ct);

            return newConfirmation;
        }

        /// <summary>
        /// Explicitly affirms an autorole assignment.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> AffirmAutoroleAsync
        (
            AutoroleConfiguration autorole,
            IUser discordUser,
            CancellationToken ct = default
        )
        {
            if (!autorole.RequiresConfirmation)
            {
                return ModifyEntityResult.FromError("The autorole doesn't require explicit affirmation.");
            }

            var getCondition = await GetOrCreateAutoroleConfirmationAsync(autorole, discordUser, ct);
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
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Explicitly affirms an autorole assignment for all currently qualifying users for the role.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> AffirmAutoroleForAllAsync
        (
            AutoroleConfiguration autorole,
            CancellationToken ct = default
        )
        {
            if (!autorole.RequiresConfirmation)
            {
                return ModifyEntityResult.FromError("The autorole doesn't require explicit affirmation.");
            }

            var qualifyingUsers = await _database.AutoroleConfirmations.ServersideQueryAsync
            (
                q => q
                    .Where(a => a.Autorole == autorole)
                    .Where(a => !a.IsConfirmed),
                ct
            );

            foreach (var qualifyingUser in qualifyingUsers)
            {
                qualifyingUser.IsConfirmed = true;
            }

            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Explicitly denies an autorole assignment.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="discordUser">The user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> DenyAutoroleAsync
        (
            AutoroleConfiguration autorole,
            IUser discordUser,
            CancellationToken ct = default
        )
        {
            if (!autorole.RequiresConfirmation)
            {
                return ModifyEntityResult.FromError("The autorole doesn't require explicit affirmation.");
            }

            var getCondition = await GetOrCreateAutoroleConfirmationAsync(autorole, discordUser, ct);
            if (!getCondition.IsSuccess)
            {
                return ModifyEntityResult.FromError(getCondition);
            }

            var condition = getCondition.Entity;
            if (!condition.IsConfirmed)
            {
                return ModifyEntityResult.FromError
                (
                    "The autorole assignment has already been denied, or has never been affirmed."
                );
            }

            condition.IsConfirmed = false;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Determines whether a given user is qualified for the given autorole.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="user">The user.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>true if the user qualifies; otherwise, false.</returns>
        public async Task<RetrieveEntityResult<bool>> IsUserQualifiedForAutoroleAsync
        (
            AutoroleConfiguration autorole,
            IGuildUser user,
            CancellationToken ct = default
        )
        {
            var results = await Task.WhenAll
            (
                autorole.Conditions.Select
                (
                    c => c.IsConditionFulfilledForUserAsync(_serviceProvider, user, ct)
                )
            );

            if (results.Any(r => !r.IsSuccess))
            {
                return RetrieveEntityResult<bool>.FromError("One or more conditions were indeterminate.");
            }

            return results.All(r => r.Entity);
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
        /// Gets all autoroles in the database, scoped to the given server.
        /// </summary>
        /// <param name="guild">The Discord guild.</param>
        /// <param name="query">Additional query statements.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>The autoroles.</returns>
        public async Task<IReadOnlyList<AutoroleConfiguration>> GetAutorolesAsync
        (
            IGuild? guild = null,
            Func<IQueryable<AutoroleConfiguration>, IQueryable<AutoroleConfiguration>>? query = null,
            CancellationToken ct = default
        )
        {
            query ??= q => q;

            if (!(guild is null))
            {
                query = q => q.Where(a => a.Server.DiscordID == (long)guild.Id);
            }

            return await _database.Autoroles.ServersideQueryAsync(query, ct);
        }

        /// <summary>
        /// Sets whether the given autorole requires external affirmation.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="requireAffirmation">Whether external affirmation is required.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetAffirmationRequiredAsync
        (
            AutoroleConfiguration autorole,
            bool requireAffirmation,
            CancellationToken ct = default
        )
        {
            if (autorole.RequiresConfirmation && requireAffirmation)
            {
                return ModifyEntityResult.FromError("The autorole already requires affirmation.");
            }

            if (!autorole.RequiresConfirmation && !requireAffirmation)
            {
                return ModifyEntityResult.FromError("The autorole already doesn't require affirmation.");
            }

            autorole.RequiresConfirmation = requireAffirmation;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets or creates server settings for the given guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<AutoroleServerSettings>> GetOrCreateServerSettingsAsync
        (
            IGuild guild,
            CancellationToken ct = default
        )
        {
            var settings = await _database.AutoroleServerSettings.ServersideQueryAsync
            (
                q => q
                    .Where(s => s.Server.DiscordID == (long)guild.Id)
                    .SingleOrDefaultAsync(ct)
            );

            if (!(settings is null))
            {
                return settings;
            }

            var getServer = await _servers.GetOrRegisterServerAsync(guild, ct);
            if (!getServer.IsSuccess)
            {
                return RetrieveEntityResult<AutoroleServerSettings>.FromError(getServer);
            }

            var server = getServer.Entity;

            var newSettings = _database.CreateProxy<AutoroleServerSettings>(server);

            _database.AutoroleServerSettings.Update(newSettings);
            await _database.SaveChangesAsync(ct);

            return newSettings;
        }

        /// <summary>
        /// Sets the affirmation notification channel for the given guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <param name="textChannel">The channel.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetAffirmationNotificationChannelAsync
        (
            IGuild guild,
            ITextChannel textChannel,
            CancellationToken ct = default
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guild, ct);
            if (!getSettings.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.AffirmationRequiredNotificationChannelID == (decimal?)textChannel.Id)
            {
                return ModifyEntityResult.FromError("That's already the affirmation notification channel.");
            }

            settings.AffirmationRequiredNotificationChannelID = (long)textChannel.Id;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Clears the affirmation notification channel for the given guild.
        /// </summary>
        /// <param name="guild">The guild.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ClearAffirmationNotificationChannelAsync
        (
            IGuild guild,
            CancellationToken ct = default
        )
        {
            var getSettings = await GetOrCreateServerSettingsAsync(guild, ct);
            if (!getSettings.IsSuccess)
            {
                return ModifyEntityResult.FromError(getSettings);
            }

            var settings = getSettings.Entity;

            if (settings.AffirmationRequiredNotificationChannelID is null)
            {
                return ModifyEntityResult.FromError("There's no affirmation notification channel set.");
            }

            settings.AffirmationRequiredNotificationChannelID = null;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Gets a query representing the users that have qualified, but have not yet been confirmed for the given
        /// role.
        /// </summary>
        /// <param name="autoroleConfiguration">The autorole.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public async Task<RetrieveEntityResult<IReadOnlyList<User>>> GetUnconfirmedUsersAsync
        (
            AutoroleConfiguration autoroleConfiguration,
            CancellationToken ct = default
        )
        {
            if (!autoroleConfiguration.RequiresConfirmation)
            {
                return RetrieveEntityResult<IReadOnlyList<User>>.FromError("The autorole doesn't require confirmation.");
            }

            return RetrieveEntityResult<IReadOnlyList<User>>.FromSuccess
            (
                await _database.AutoroleConfirmations.ServersideQueryAsync
                (
                    q => q
                    .Where(ac => ac.Autorole == autoroleConfiguration)
                    .Where(ac => !ac.IsConfirmed)
                    .Select(ac => ac.User),
                    ct
                )
            );
        }

        /// <summary>
        /// Modifies the given condition using the given function, saving the results after.
        /// </summary>
        /// <param name="condition">The condition to modify.</param>
        /// <param name="modificationAction">The action to apply.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <typeparam name="TCondition">The condition type.</typeparam>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> ModifyConditionAsync<TCondition>
        (
            TCondition condition,
            Action<TCondition> modificationAction,
            CancellationToken ct = default
        )
            where TCondition : AutoroleCondition
        {
            modificationAction(condition);
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Sets whether the notification for the given confirmation has been sent.
        /// </summary>
        /// <param name="autoroleConfirmation">The confirmation.</param>
        /// <param name="hasNotificationBeenSent">Whether the notification has been sent.</param>
        /// <param name="ct">The cancellation token in use.</param>
        /// <returns>A modification result which may or may not have succeeded.</returns>
        public async Task<ModifyEntityResult> SetHasNotificationBeenSentAsync
        (
            AutoroleConfirmation autoroleConfirmation,
            bool hasNotificationBeenSent,
            CancellationToken ct = default
        )
        {
            autoroleConfirmation.HasNotificationBeenSent = hasNotificationBeenSent;
            await _database.SaveChangesAsync(ct);

            return ModifyEntityResult.FromSuccess();
        }
    }
}
