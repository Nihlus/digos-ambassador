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

using System;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Amby;
using DIGOS.Ambassador.Plugins.Amby.Behaviours;
using DIGOS.Ambassador.Plugins.Amby.CommandModules;
using DIGOS.Ambassador.Plugins.Amby.Services;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Behaviours.Services;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(AmbyPlugin))]

namespace DIGOS.Ambassador.Plugins.Amby
{
    /// <summary>
    /// Describes the Amby plugin.
    /// </summary>
    [PublicAPI]
    public sealed class AmbyPlugin : PluginDescriptor
    {
        /// <inheritdoc />
        public override string Name => "Amby";

        /// <inheritdoc />
        public override string Description => "Contains various Amby-specific commands.";

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<PortraitService>()
                .AddSingleton<SassService>();
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            var commands = serviceProvider.GetRequiredService<CommandService>();
            await commands.AddModuleAsync<AmbyCommands>(serviceProvider);

            var behaviours = serviceProvider.GetRequiredService<BehaviourService>();
            await behaviours.AddBehaviourAsync<AmbassadorCommandBehaviour>(serviceProvider);

            return true;
        }
    }
}
