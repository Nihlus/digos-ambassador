//
//  StatisticsPlugin.cs
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

using DIGOS.Ambassador.Plugins.Statistics;
using DIGOS.Ambassador.Plugins.Statistics.CommandGroups;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(StatisticsPlugin))]

namespace DIGOS.Ambassador.Plugins.Statistics;

/// <summary>
/// Describes the Statistics plugin.
/// </summary>
public sealed class StatisticsPlugin : PluginDescriptor
{
    /// <inheritdoc />
    public override string Name => "Statistics";

    /// <inheritdoc />
    public override string Description => "Provides various commands for view statistics about the bot.";

    /// <inheritdoc />
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddCommandTree()
                .WithCommandGroup<StatCommands>();

        return Result.FromSuccess();
    }
}
