//
//  AmbyPlugin.cs
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

using DIGOS.Ambassador.Plugins.Amby;
using DIGOS.Ambassador.Plugins.Amby.CommandModules;
using DIGOS.Ambassador.Plugins.Amby.Services;
using Microsoft.Extensions.DependencyInjection;
using Remora.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(AmbyPlugin))]

namespace DIGOS.Ambassador.Plugins.Amby;

/// <summary>
/// Describes the Amby plugin.
/// </summary>
public sealed class AmbyPlugin : PluginDescriptor
{
    /// <inheritdoc />
    public override string Name => "Amby";

    /// <inheritdoc />
    public override string Description => "Contains various Amby-specific commands.";

    /// <inheritdoc/>
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<PortraitService>()
            .AddSingleton<SassService>();

        serviceCollection.AddCommandGroup<AmbyCommands>();

        return Result.FromSuccess();
    }
}
