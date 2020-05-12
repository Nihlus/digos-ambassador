//
//  AutorolePlugin.cs
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
using DIGOS.Ambassador.Discord.Interactivity.Behaviours;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Autorole;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Autorole.TypeReaders;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Discord;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Behaviours;
using Remora.Behaviours.Services;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using AutoroleCommands = DIGOS.Ambassador.Plugins.Autorole.CommandModules.AutoroleCommands;

[assembly: InternalsVisibleTo("DIGOS.Ambassador.Tests.Plugins.Autorole")]
[assembly: RemoraPlugin(typeof(AutorolePlugin))]

namespace DIGOS.Ambassador.Plugins.Autorole
{
    /// <summary>
    /// Describes the character plugin.
    /// </summary>
    public sealed class AutorolePlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Autorole";

        /// <inheritdoc />
        public override string Description => "Allows administrators to create automated role assignments.";

        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<AutoroleService>()
                .AddConfiguredSchemaAwareDbContextPool<AutoroleDatabaseContext>();
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
            commands.AddTypeReader<AutoroleConfiguration>(new AutoroleTypeReader());
            commands.AddTypeReader<IEmote>(new EmojiTypeReader());

            await commands.AddModuleAsync<AutoroleCommands>(serviceProvider);

            var behaviourService = serviceProvider.GetRequiredService<BehaviourService>();
            await behaviourService.AddBehaviourAsync<InteractivityBehaviour>(serviceProvider);
            await behaviourService.AddBehaviourAsync<DelayedActionBehaviour>(serviceProvider);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AutoroleDatabaseContext>();

            await context.Database.MigrateAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> HasCreatedPersistentStoreAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<AutoroleDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
