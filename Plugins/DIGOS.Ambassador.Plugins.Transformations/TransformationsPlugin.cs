//
//  TransformationsPlugin.cs
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
using DIGOS.Ambassador.Database.Abstractions.Extensions;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Plugins.Abstractions;
using DIGOS.Ambassador.Plugins.Abstractions.Attributes;
using DIGOS.Ambassador.Plugins.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.CommandModules;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Services.Lua;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.TypeReaders;
using Discord.Commands;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

[assembly: AmbassadorPlugin(typeof(TransformationsPlugin))]

namespace DIGOS.Ambassador.Plugins.Transformations
{
    /// <summary>
    /// Describes the transformation plugin.
    /// </summary>
    public class TransformationsPlugin : PluginDescriptor, IMigratablePlugin
    {
        /// <inheritdoc />
        public override string Name => "Transformations";

        /// <inheritdoc />
        public override string Description => "Provides user-managed transformation libraries.";

        /// <inheritdoc />
        public override Task<bool> RegisterServicesAsync(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddScoped<LuaService>()
                .AddScoped<TransformationService>()
                .AddSchemaAwareDbContextPool<TransformationsDatabaseContext>();

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            var commands = serviceProvider.GetRequiredService<CommandService>();

            commands.AddTypeReader<Colour>(new ColourTypeReader());

            commands.AddEnumReader<Bodypart>();
            commands.AddEnumReader<Pattern>();

            await commands.AddModuleAsync<TransformationCommands>(serviceProvider);

            var transformationService = serviceProvider.GetRequiredService<TransformationService>();
            transformationService.WithDescriptionBuilder
            (
                ActivatorUtilities.CreateInstance<TransformationDescriptionBuilder>(serviceProvider)
            );

            return true;
        }

        /// <inheritdoc />
        public async Task<bool> MigratePluginAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<TransformationsDatabaseContext>();

            await context.Database.MigrateAsync();

            return true;
        }
    }
}
