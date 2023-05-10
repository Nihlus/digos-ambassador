//
//  AuctionsPlugin.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Plugins.Auctions;
using DIGOS.Ambassador.Plugins.Auctions.Autocomplete;
using DIGOS.Ambassador.Plugins.Auctions.Commands;
using DIGOS.Ambassador.Plugins.Auctions.Interactions;
using DIGOS.Ambassador.Plugins.Auctions.Model;
using DIGOS.Ambassador.Plugins.Auctions.Parsers;
using DIGOS.Ambassador.Plugins.Auctions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Interactivity.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(AuctionsPlugin))]

namespace DIGOS.Ambassador.Plugins.Auctions;

/// <summary>
/// Describes the Auctions plugin.
/// </summary>
public sealed class AuctionsPlugin : PluginDescriptor, IMigratablePlugin
{
    /// <inheritdoc />
    public override string Name => "Auctions";

    /// <inheritdoc />
    public override string Description => "Contains various Auctions-specific commands.";

    /// <inheritdoc/>
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddConfiguredSchemaAwareDbContextPool<AuctionsDatabaseContext>();

        serviceCollection
            .AddCommandTree()
                .WithCommandGroup<AuctionCommands>();

        serviceCollection
            .AddInteractionGroup<AuctionModals>();

        serviceCollection
            .AddAutocompleteProvider<AuctionAutocompleteProvider>()
            .AddParser<AuctionParser>();

        serviceCollection
            .AddScoped<AuctionDisplayService>();

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public async Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var context = serviceProvider.GetRequiredService<AuctionsDatabaseContext>();

        await context.Database.MigrateAsync(ct);

        return Result.FromSuccess();
    }
}
