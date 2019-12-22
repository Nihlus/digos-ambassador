//
//  RoleplayServiceTestBase.cs
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
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using DIGOS.Ambassador.Tests.Extensions;
using DIGOS.Ambassador.Tests.TestBases;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Roleplaying
{
    /// <summary>
    /// Serves as a test base for roleplay service tests.
    /// </summary>
    public abstract class RoleplayServiceTestBase : DatabaseProvidingTestBase
    {
        /// <summary>
        /// Gets the database.
        /// </summary>
        public RoleplayingDatabaseContext Database { get; private set; } = null!;

        /// <summary>
        /// Gets the roleplay service object.
        /// </summary>
        protected RoleplayService Roleplays { get; private set; } = null!;

        /// <inheritdoc />
        protected override void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddDbContext<CoreDatabaseContext>(ConfigureOptions<CoreDatabaseContext>)
                .AddDbContext<RoleplayingDatabaseContext>(ConfigureOptions<RoleplayingDatabaseContext>);

            serviceCollection
                .AddScoped<CommandService>()
                .AddScoped<OwnedEntityService>()
                .AddScoped<UserService>()
                .AddScoped<ServerService>()
                .AddScoped<RoleplayService>();
        }

        /// <inheritdoc />
        protected override void ConfigureServices(IServiceProvider serviceProvider)
        {
            var coreDatabase = serviceProvider.GetRequiredService<CoreDatabaseContext>();
            coreDatabase.Database.Create();

            this.Database = serviceProvider.GetRequiredService<RoleplayingDatabaseContext>();
            this.Database.Database.Create();

            this.Roleplays = serviceProvider.GetRequiredService<RoleplayService>();
        }
    }
}
