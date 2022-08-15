//
//  TransformationsPlugin.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.CommandModules;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Services.Lua;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.TypeParsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Commands.Parsers;
using Remora.Discord.Pagination.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: InternalsVisibleTo("DIGOS.Ambassador.Tests.Plugins.Transformations")]
[assembly: RemoraPlugin(typeof(TransformationsPlugin))]

namespace DIGOS.Ambassador.Plugins.Transformations;

/// <summary>
/// Describes the transformation plugin.
/// </summary>
public sealed class TransformationsPlugin : PluginDescriptor, IMigratablePlugin
{
    /// <inheritdoc />
    public override string Name => "Transformations";

    /// <inheritdoc />
    public override string Description => "Provides user-managed transformation services.";

    /// <inheritdoc />
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddPagination();

        serviceCollection.TryAddSingleton<TransformationDescriptionBuilder>();
        serviceCollection.TryAddSingleton(services =>
        {
            var contentService = services.GetRequiredService<ContentService>();
            var getTransformationText = contentService.GetTransformationMessages();

            if (!getTransformationText.IsSuccess)
            {
                throw new InvalidOperationException("Failed to load the transformation messages.");
            }

            return getTransformationText.Entity;
        });

        serviceCollection.TryAddScoped<LuaService>();
        serviceCollection.TryAddScoped<TransformationService>();
        serviceCollection.AddConfiguredSchemaAwareDbContextPool<TransformationsDatabaseContext>();

        serviceCollection.AddParser<ColourTypeParser>();
        serviceCollection.AddParser<EnumParser<Pattern>>();
        serviceCollection.AddParser<EnumParser<Shade>>();
        serviceCollection.AddParser<EnumParser<ShadeModifier>>();
        serviceCollection.AddParser<EnumParser<ProtectionType>>();
        serviceCollection.AddParser<EnumParser<Chirality>>();

        serviceCollection.AddParser<ColourTypeParser>();
        serviceCollection
            .AddCommandTree()
                .WithCommandGroup<TransformationCommands>();

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var context = serviceProvider.GetRequiredService<TransformationsDatabaseContext>();

        await context.Database.MigrateAsync(ct);
        return Result.FromSuccess();
    }
}
