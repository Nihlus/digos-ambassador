//
//  ServiceCollectionExtensions.cs
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

using System.Linq;
using DIGOS.Ambassador.Discord.Interactivity.Responders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Gateway.Responders;

namespace DIGOS.Ambassador.Discord.Interactivity.Extensions;

/// <summary>
/// Provides extension methods for the <see cref="IServiceCollection"/> interface.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the services required for interactivity.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <returns>The collection, with the added services.</returns>
    public static IServiceCollection AddInteractivity(this IServiceCollection serviceCollection)
    {
        serviceCollection.Configure<DiscordGatewayClientOptions>(o =>
        {
            o.Intents |= GatewayIntents.DirectMessageReactions;
            o.Intents |= GatewayIntents.GuildMessageReactions;
        });

        serviceCollection.TryAddSingleton<InteractiveMessageTracker>();
        serviceCollection.TryAddScoped<InteractivityService>();
        serviceCollection.AddResponder<MessageDeletionResponder>();

        return serviceCollection;
    }

    /// <summary>
    /// Adds a responder as an interactivity responder, if it is not already registered.
    /// </summary>
    /// <param name="serviceCollection">The service collection.</param>
    /// <typeparam name="TResponder">The responder type.</typeparam>
    /// <returns>The collection, with the added services.</returns>
    public static IServiceCollection TryAddInteractivityResponder<TResponder>
    (
        this IServiceCollection serviceCollection
    )
        where TResponder : InteractivityResponder, IResponder
    {
        if (serviceCollection.Any(s => s.ImplementationType == typeof(TResponder)))
        {
            return serviceCollection;
        }

        serviceCollection.AddResponder<TResponder>();
        serviceCollection.AddScoped<InteractivityResponder, TResponder>();

        return serviceCollection;
    }
}
