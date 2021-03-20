//
//  PermissionsPlugin.cs
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
using System.Reflection;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Interactivity.Extensions;
using DIGOS.Ambassador.Discord.Pagination.Responders;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Permissions;
using DIGOS.Ambassador.Plugins.Permissions.CommandModules;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(PermissionsPlugin))]

namespace DIGOS.Ambassador.Plugins.Permissions
{
    /// <summary>
    /// Describes the permission plugin.
    /// </summary>
    public sealed class PermissionsPlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Permissions";

        /// <inheritdoc />
        public override string Description => "Provides granular permissions for commands and functionality.";

        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            // Dependencies
            serviceCollection.TryAddSingleton<UserFeedbackService>();
            serviceCollection.AddInteractivity();
            serviceCollection.TryAddInteractivityResponder<PaginatedMessageResponder>();

            // Our stuff
            serviceCollection.TryAddSingleton<PermissionRegistryService>();
            serviceCollection.TryAddScoped<PermissionService>();

            serviceCollection.AddConfiguredSchemaAwareDbContextPool<PermissionsDatabaseContext>();

            serviceCollection.AddCondition<RequirePermissionCondition>();
            serviceCollection.AddCommandGroup<PermissionCommands>();
            serviceCollection.AddParser<PermissionTarget, HumanizerEnumTypeReader<PermissionTarget>>();
        }

        /// <inheritdoc />
        public override ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider)
        {
            var permissionRegistry = serviceProvider.GetRequiredService<PermissionRegistryService>();
            return new ValueTask<Result>(permissionRegistry.RegisterPermissions
            (
                Assembly.GetExecutingAssembly(),
                serviceProvider
            ));
        }

        /// <inheritdoc />
        public async Task<Result> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<PermissionsDatabaseContext>();

            await context.Database.MigrateAsync();

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<bool> HasCreatedPersistentStoreAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<PermissionsDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
