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

using DIGOS.Ambassador.Core.Database.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.EntityFrameworkCore.Modular;
using Remora.EntityFrameworkCore.Modular.Extensions;
using Remora.EntityFrameworkCore.Modular.Services;

namespace DIGOS.Ambassador.Core.Database.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="IServiceCollection"/> interface.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds and configures a database pool for the given schema-aware context type.
        /// </summary>
        /// <param name="this">The service collection.</param>
        /// <typeparam name="TContext">The context type.</typeparam>
        /// <returns>The service collection, with the pool added.</returns>
            public static IServiceCollection AddConfiguredSchemaAwareDbContextPool<TContext>
        (
            this IServiceCollection @this
        )
            where TContext : SchemaAwareDbContext
        {
            @this.TryAddSingleton<SchemaAwareDbContextService>();
            @this.TryAddSingleton<ContextConfigurationService>();

            return @this.AddSchemaAwareDbContextPool<TContext>((provider, builder) =>
            {
                var configurationService = provider.GetRequiredService<ContextConfigurationService>();
                configurationService.ConfigureSchemaAwareContext<TContext>(builder);
            });
        }
    }
}
