//
//  QuotesPlugin.cs
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
using System.Reflection;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Quotes;
using DIGOS.Ambassador.Plugins.Quotes.Behaviours;
using DIGOS.Ambassador.Plugins.Quotes.Services;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Remora.Behaviours.Extensions;
using Remora.Behaviours.Services;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;

[assembly: RemoraPlugin(typeof(QuotesPlugin))]

namespace DIGOS.Ambassador.Plugins.Quotes
{
    /// <summary>
    /// Describes the Quotes plugin.
    /// </summary>
    [PublicAPI]
    public sealed class QuotesPlugin : PluginDescriptor
    {
        /// <inheritdoc />
        public override string Name => "Quotes";

        /// <inheritdoc />
        public override string Description => "Provides automatic conversion of message links to quotes.";

        /// <inheritdoc/>
        public override void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddSingleton<QuoteService>()
                .AddBehaviour<MessageQuoteBehaviour>();
        }
    }
}
