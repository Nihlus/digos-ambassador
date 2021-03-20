//
//  JoinMessagesPlugin.cs
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

using DIGOS.Ambassador.Plugins.JoinMessages;
using DIGOS.Ambassador.Plugins.JoinMessages.Responders;
using Microsoft.Extensions.DependencyInjection;
using Remora.Discord.Gateway.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(JoinMessagesPlugin))]

namespace DIGOS.Ambassador.Plugins.JoinMessages
{
    /// <summary>
    /// Describes the JoinMessages plugin.
    /// </summary>
    public sealed class JoinMessagesPlugin : PluginDescriptor
    {
        /// <inheritdoc />
        public override string Name => "JoinMessages";

        /// <inheritdoc />
        public override string Description => "Sends initial join messages to new guild members.";

        /// <inheritdoc />
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddResponder<JoinMessageResponder>();
        }
    }
}
