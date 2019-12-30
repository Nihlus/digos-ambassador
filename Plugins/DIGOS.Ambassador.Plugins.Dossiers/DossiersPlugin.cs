//
//  DossiersPlugin.cs
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
using DIGOS.Ambassador.Plugins.Abstractions.Database;
using DIGOS.Ambassador.Plugins.Dossiers;
using DIGOS.Ambassador.Plugins.Dossiers.CommandModules;
using DIGOS.Ambassador.Plugins.Dossiers.Model;
using DIGOS.Ambassador.Plugins.Dossiers.Services;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using Microsoft.Extensions.DependencyInjection;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(DossiersPlugin))]

namespace DIGOS.Ambassador.Plugins.Dossiers
{
    /// <summary>
    /// Describes the Dossiers plugin.
    /// </summary>
    [PublicAPI]
    public sealed class DossiersPlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Dossiers";

        /// <inheritdoc />
        public override string Description => "Provides a way to store and view dossiers about DIGOS units.";

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddConfiguredSchemaAwareDbContextPool<DossiersDatabaseContext>();
            serviceCollection.AddSingleton<DossierService>();
        }

        /// <inheritdoc/>
        public override async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            var commandService = serviceProvider.GetRequiredService<CommandService>();
            await commandService.AddModuleAsync<DossierCommands>(serviceProvider);

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<DossiersDatabaseContext>();

            await context.Database.MigrateAsync();

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> IsDatabaseCreatedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<DossiersDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
