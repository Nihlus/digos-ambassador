//
//  PermissionServiceTestBase.cs
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
using DIGOS.Ambassador.Plugins.Permissions.Model;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using DIGOS.Ambassador.Tests.Extensions;
using DIGOS.Ambassador.Tests.TestBases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Permissions
{
    /// <summary>
    /// Serves as a test base for permission service tests.
    /// </summary>
    [PublicAPI]
    public abstract class PermissionServiceTestBase : DatabaseProvidingTestBase
    {
        /// <summary>
        /// Gets the permission service instance.
        /// </summary>
        protected PermissionService Permissions { get; private set; } = null!;

        /// <summary>
        /// Gets the permission database.
        /// </summary>
        protected PermissionsDatabaseContext Database { get; private set; } = null!;

        /// <inheritdoc />
        protected sealed override void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddDbContext<PermissionsDatabaseContext>(ConfigureOptions<PermissionsDatabaseContext>);

            serviceCollection
                .AddScoped<PermissionService>()
                .AddLogging(c => c.AddProvider(NullLoggerProvider.Instance));
        }

        /// <inheritdoc />
        protected sealed override void ConfigureServices(IServiceProvider serviceProvider)
        {
            this.Database = serviceProvider.GetRequiredService<PermissionsDatabaseContext>();
            this.Database.Database.Create();

            this.Permissions = serviceProvider.GetRequiredService<PermissionService>();
        }
    }
}
