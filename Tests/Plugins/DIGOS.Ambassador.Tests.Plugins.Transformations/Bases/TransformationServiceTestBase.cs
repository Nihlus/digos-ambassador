//
//  TransformationServiceTestBase.cs
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
using DIGOS.Ambassador.Core.Database;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Tests.Extensions;
using DIGOS.Ambassador.Tests.TestBases;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    /// <summary>
    /// Serves as a test base for transformation service tests.
    /// </summary>
    public abstract class TransformationServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
    {
        /// <summary>
        /// Gets the database.
        /// </summary>
        protected TransformationsDatabaseContext Database { get; private set; } = null!;

        /// <summary>
        /// Gets the character database.
        /// </summary>
        protected CharactersDatabaseContext CharacterDatabase { get; private set; } = null!;

        /// <summary>
        /// Gets the core database.
        /// </summary>
        protected CoreDatabaseContext CoreDatabase { get; private set; } = null!;

        /// <summary>
        /// Gets the transformation service object.
        /// </summary>
        protected TransformationService Transformations { get; private set; } = null!;

        /// <summary>
        /// Gets the user service.
        /// </summary>
        protected UserService Users { get; private set; } = null!;

        /// <summary>
        /// Gets the server service.
        /// </summary>
        protected ServerService Servers { get; private set; } = null!;

        /// <inheritdoc />
        protected override void RegisterServices(IServiceCollection serviceCollection)
        {
            serviceCollection
                .AddDbContext<CoreDatabaseContext>(ConfigureOptions<CoreDatabaseContext>)
                .AddDbContext<TransformationsDatabaseContext>(ConfigureOptions<TransformationsDatabaseContext>)
                .AddDbContext<CharactersDatabaseContext>(ConfigureOptions<CharactersDatabaseContext>);

            serviceCollection
                .AddSingleton(s =>
                {
                    var content = s.GetRequiredService<ContentService>();
                    return content.GetTransformationMessages().Entity;
                })
                .AddSingleton(FileSystemFactory.CreateContentFileSystem())
                .AddSingleton<PronounService>()
                .AddSingleton<TransformationDescriptionBuilder>()
                .AddScoped<TransformationService>()
                .AddScoped<ContentService>()
                .AddScoped<UserService>()
                .AddScoped<ServerService>()
                .AddScoped<UserService>()
                .AddLogging(c => c.AddProvider(NullLoggerProvider.Instance));
        }

        /// <inheritdoc />
        protected override void ConfigureServices(IServiceProvider serviceProvider)
        {
            this.CoreDatabase = serviceProvider.GetRequiredService<CoreDatabaseContext>();
            this.CoreDatabase.Database.Create();

            this.CharacterDatabase = serviceProvider.GetRequiredService<CharactersDatabaseContext>();
            this.CharacterDatabase.Database.Create();

            this.Database = serviceProvider.GetRequiredService<TransformationsDatabaseContext>();
            this.Database.Database.Create();

            this.Transformations = serviceProvider.GetRequiredService<TransformationService>();
            this.Users = serviceProvider.GetRequiredService<UserService>();
            this.Servers = serviceProvider.GetRequiredService<ServerService>();

            var pronouns = serviceProvider.GetRequiredService<PronounService>();
            pronouns.WithPronounProvider(new TheyPronounProvider());
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            await this.Transformations.UpdateTransformationDatabaseAsync();

            await InitializeTestAsync();
        }

        /// <summary>
        /// Initializes the test data.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected virtual Task InitializeTestAsync()
        {
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public virtual async Task DisposeAsync()
        {
            this.Transformations.SaveChanges();
            await this.Transformations.DisposeAsync();
        }
    }
}
