//
//  ModerationPlugin.cs
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
using System.Reflection;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Plugins.Abstractions.Database;
using DIGOS.Ambassador.Plugins.Moderation;
using DIGOS.Ambassador.Plugins.Moderation.CommandModules;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Remora.Behaviours.Services;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(ModerationPlugin))]

namespace DIGOS.Ambassador.Plugins.Moderation
{
    /// <summary>
    /// Describes the moderation plugin.
    /// </summary>
    public class ModerationPlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Moderation";

        /// <inheritdoc />
        public override string Description => "Provides simple moderation tools.";

        /// <inheritdoc />
        public override Task<bool> RegisterServicesAsync(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSchemaAwareDbContextPool<ModerationDatabaseContext>();

            serviceCollection
                .AddScoped<ModerationService>()
                .AddScoped<NoteService>()
                .AddScoped<WarningService>()
                .AddScoped<BanService>()
                .AddScoped<ChannelLoggingService>();

            return Task.FromResult(true);
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
            await commands.AddModuleAsync<ModerationCommands>(serviceProvider);
            await commands.AddModuleAsync<NoteCommands>(serviceProvider);
            await commands.AddModuleAsync<WarningCommands>(serviceProvider);
            await commands.AddModuleAsync<BanCommands>(serviceProvider);

            var behaviours = serviceProvider.GetRequiredService<BehaviourService>();
            await behaviours.AddBehavioursAsync(Assembly.GetExecutingAssembly(), serviceProvider);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ModerationDatabaseContext>();

            await context.Database.MigrateAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> IsDatabaseCreatedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ModerationDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
