//
//  RoleplayingPlugin.cs
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
using DIGOS.Ambassador.Discord.Interactivity.Behaviours;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using DIGOS.Ambassador.Plugins.Roleplaying;
using DIGOS.Ambassador.Plugins.Roleplaying.CommandModules;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Plugins.Roleplaying.TypeReaders;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Behaviours;
using Remora.Behaviours.Services;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(RoleplayingPlugin))]

namespace DIGOS.Ambassador.Plugins.Roleplaying
{
    /// <summary>
    /// Describes the roleplay plugin.
    /// </summary>
    [PublicAPI]
    public sealed class RoleplayingPlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Roleplays";

        /// <inheritdoc />
        public override string Description => "Provides user-managed roleplay libraries.";

        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<RoleplayService>()
                .AddScoped<RoleplayServerSettingsService>()
                .AddScoped<DedicatedChannelService>()
                .AddConfiguredSchemaAwareDbContextPool<RoleplayingDatabaseContext>();
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            var permissionRegistry = serviceProvider.GetRequiredService<PermissionRegistryService>();
            var registrationResult = permissionRegistry.RegisterPermissions
            (
                Assembly.GetExecutingAssembly(),
                serviceProvider
            );

            if (!registrationResult.IsSuccess)
            {
                return false;
            }

            var commands = serviceProvider.GetRequiredService<CommandService>();

            commands.AddTypeReader<IMessage>(new UncachedMessageTypeReader<IMessage>(), true);
            commands.AddTypeReader<Roleplay>(new RoleplayTypeReader());

            await commands.AddModuleAsync<RoleplayCommands>(serviceProvider);

            var behaviours = serviceProvider.GetRequiredService<BehaviourService>();
            await behaviours.AddBehavioursAsync(Assembly.GetExecutingAssembly(), serviceProvider);
            await behaviours.AddBehaviourAsync<InteractivityBehaviour>(serviceProvider);
            await behaviours.AddBehaviourAsync<DelayedActionBehaviour>(serviceProvider);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<RoleplayingDatabaseContext>();

            await context.Database.MigrateAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> HasCreatedPersistentStoreAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<RoleplayingDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
