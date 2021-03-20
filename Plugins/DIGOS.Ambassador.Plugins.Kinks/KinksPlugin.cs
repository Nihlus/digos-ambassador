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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Discord.Interactivity.Extensions;
using DIGOS.Ambassador.Discord.Pagination.Responders;
using DIGOS.Ambassador.Plugins.Kinks;
using DIGOS.Ambassador.Plugins.Kinks.CommandModules;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Responders;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Commands.Parsers;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(KinksPlugin))]

namespace DIGOS.Ambassador.Plugins.Kinks
{
    /// <summary>
    /// Describes the kink plugin.
    /// </summary>
    [PublicAPI]
    public sealed class KinksPlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Kinks";

        /// <inheritdoc />
        public override string Description => "Provides user-managed kink libraries.";

        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.TryAddScoped<KinkService>();
            serviceCollection.AddConfiguredSchemaAwareDbContextPool<KinksDatabaseContext>();
            serviceCollection.TryAddInteractivityResponder<PaginatedMessageResponder>();
            serviceCollection.TryAddInteractivityResponder<KinkWizardResponder>();

            serviceCollection.AddCommandGroup<KinkCommands>();

            serviceCollection.AddParser<KinkPreference, EnumParser<KinkPreference>>();
        }

        /// <inheritdoc />
        public async Task<Result> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<KinksDatabaseContext>();

            await context.Database.MigrateAsync();

            return Result.FromSuccess();
        }

        /// <inheritdoc />
        public async Task<bool> HasCreatedPersistentStoreAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<KinksDatabaseContext>();
            var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();

            return appliedMigrations.Any();
        }
    }
}
