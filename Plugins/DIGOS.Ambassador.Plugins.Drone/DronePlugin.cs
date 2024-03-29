﻿//
//  DronePlugin.cs
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
using DIGOS.Ambassador.Plugins.Drone;
using DIGOS.Ambassador.Plugins.Drone.CommandModules;
using DIGOS.Ambassador.Plugins.Drone.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(DronePlugin))]

namespace DIGOS.Ambassador.Plugins.Drone;

/// <summary>
/// Describes the Drone plugin.
/// </summary>
public sealed class DronePlugin : PluginDescriptor
{
    /// <inheritdoc />
    public override string Name => "Drone";

    /// <inheritdoc />
    public override string Description => "Provides a single command to drone people.";

    /// <inheritdoc/>
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddScoped<DroneService>();
        serviceCollection.TryAddSingleton<Random>();

        serviceCollection.AddCommandTree()
            .WithCommandGroup<DroneCommands>();

        return Result.FromSuccess();
    }
}
