//
//  KinksPlugin.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Abstractions;
using DIGOS.Ambassador.Plugins.Abstractions.Attributes;
using DIGOS.Ambassador.Plugins.Kinks;
using DIGOS.Ambassador.Plugins.Kinks.CommandModules;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;

[assembly: AmbassadorPlugin(typeof(KinksPlugin))]

namespace DIGOS.Ambassador.Plugins.Kinks
{
    /// <summary>
    /// Describes the kink plugin.
    /// </summary>
    public class KinksPlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Kinks";

        /// <inheritdoc />
        public override string Description => "Provides user-managed kink libraries.";

        /// <inheritdoc />
        public override Task<bool> RegisterServicesAsync(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<KinkService>()
                .AddSchemaAwareDbContextPool<KinksDatabaseContext>();

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            var commands = serviceProvider.GetRequiredService<CommandService>();

            commands.AddEnumReader<KinkPreference>();

            await commands.AddModuleAsync<KinkCommands>(serviceProvider);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<KinksDatabaseContext>();

            await context.Database.MigrateAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> IsDatabaseCreatedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<KinksDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
