//
//  CorePlugin.cs
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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Plugins.Core;
using DIGOS.Ambassador.Plugins.Core.CommandModules;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Responders;
using DIGOS.Ambassador.Plugins.Core.Services;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: InternalsVisibleTo("DIGOS.Ambassador.Tests.Plugins.Core")]
[assembly: RemoraPlugin(typeof(CorePlugin))]

namespace DIGOS.Ambassador.Plugins.Core
{
    /// <summary>
    /// Describes the plugin containing core functionality.
    /// </summary>
    public sealed class CorePlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Core";

        /// <inheritdoc />
        public override string Description => "Provides core functionality related to users and servers.";

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddSingleton<IdentityInformationService>();
            serviceCollection.TryAddScoped<ServerService>();
            serviceCollection.TryAddScoped<UserService>();
            serviceCollection.TryAddScoped<PrivacyService>();
            serviceCollection.TryAddScoped<OwnedEntityService>();
            serviceCollection.AddConfiguredSchemaAwareDbContextPool<CoreDatabaseContext>();

            serviceCollection
                .AddCommandGroup<PrivacyCommands>()
                .AddCommandGroup<UserCommands>()
                .AddCommandGroup<ServerCommands>();

            serviceCollection.AddResponder<ReadyResponder>();
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
            var context = serviceProvider.GetRequiredService<CoreDatabaseContext>();

            await context.Database.MigrateAsync();

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<bool> HasCreatedPersistentStoreAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<CoreDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
