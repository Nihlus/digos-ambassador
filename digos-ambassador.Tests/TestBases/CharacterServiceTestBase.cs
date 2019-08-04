//
//  CharacterServiceTestBase.cs
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
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Services;
using Discord.Commands;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace DIGOS.Ambassador.Tests.TestBases
{
    /// <summary>
    /// Serves as a test base for character service tests.
    /// </summary>
    public abstract class CharacterServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
    {
        /// <summary>
        /// Gets the database.
        /// </summary>
        protected AmbyDatabaseContext Database { get; private set; }

        /// <summary>
        /// Gets the character service object.
        /// </summary>
        protected CharacterService Characters { get; private set; }

        /// <summary>
        /// Gets the user service.
        /// </summary>
        protected UserService Users { get; private set; }

        /// <summary>
        /// Gets the command service dependency.
        /// </summary>
        protected CommandService Commands { get; private set; }

        /// <inheritdoc />
        protected override void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddDbContext<CoreDatabaseContext>(ConfigureOptions<CoreDatabaseContext>)
                .AddDbContext<AmbyDatabaseContext>(ConfigureOptions<AmbyDatabaseContext>);

            serviceCollection
                .AddScoped<CommandService>()
                .AddScoped<ContentService>()
                .AddScoped<ServerService>()
                .AddScoped<TransformationService>()
                .AddScoped<UserService>()
                .AddScoped<OwnedEntityService>()
                .AddScoped<CharacterService>();
        }

        /// <inheritdoc />
        protected override void ConfigureServices(IServiceProvider serviceProvider)
        {
            var coreDatabase = serviceProvider.GetRequiredService<CoreDatabaseContext>();
            coreDatabase.Database.EnsureCreated();

            var ambyDatabase = serviceProvider.GetRequiredService<AmbyDatabaseContext>();
            ambyDatabase.Database.EnsureCreated();

            this.Database = ambyDatabase;

            this.Characters = serviceProvider.GetRequiredService<CharacterService>();
            this.Users = serviceProvider.GetRequiredService<UserService>();
            this.Commands = serviceProvider.GetRequiredService<CommandService>();
        }

        /// <inheritdoc />
        public virtual async Task InitializeAsync()
        {
            var transformations = this.Services.GetRequiredService<TransformationService>();
            var db = this.Services.GetRequiredService<AmbyDatabaseContext>();

            await transformations.UpdateTransformationDatabaseAsync(db);
        }

        /// <inheritdoc />
        public virtual Task DisposeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
