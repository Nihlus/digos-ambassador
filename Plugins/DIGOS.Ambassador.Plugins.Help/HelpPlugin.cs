//
//  HelpPlugin.cs
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
using DIGOS.Ambassador.Plugins.Help;
using DIGOS.Ambassador.Plugins.Help.CommandModules;
using DIGOS.Ambassador.Plugins.Help.Services;
using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(HelpPlugin))]

namespace DIGOS.Ambassador.Plugins.Help
{
    /// <summary>
    /// Describes the Help plugin.
    /// </summary>
    [PublicAPI]
    public sealed class HelpPlugin : PluginDescriptor
    {
        /// <inheritdoc />
        public override string Name => "Help";

        /// <inheritdoc />
        public override string Description => "Provides an interactive manual.";

        /// <inheritdoc />
        public override Task<bool> RegisterServicesAsync(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<HelpService>();

            return Task.FromResult(true);
        }

        /// <inheritdoc />
        public override async Task<bool> InitializeAsync(IServiceProvider serviceProvider)
        {
            var commandService = serviceProvider.GetRequiredService<CommandService>();
            await commandService.AddModuleAsync<HelpCommands>(serviceProvider);

            return true;
        }
    }
}
