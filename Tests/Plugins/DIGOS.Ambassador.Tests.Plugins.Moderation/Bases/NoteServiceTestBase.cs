//
//  NoteServiceTestBase.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Tests.TestBases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Bases;

/// <summary>
/// Serves as a test base for note service tests.
/// </summary>
[PublicAPI]
public class NoteServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
{
    /// <summary>
    /// Gets the database context.
    /// </summary>
    protected ModerationDatabaseContext Database { get; private set; } = null!;

    /// <summary>
    /// Gets the note service.
    /// </summary>
    protected NoteService Notes { get; private set; } = null!;

    /// <inheritdoc />
    protected override void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddDbContext<CoreDatabaseContext>(o => ConfigureOptions(o, "Core"))
            .AddDbContext<ModerationDatabaseContext>(o => ConfigureOptions(o, "Moderation"));

        serviceCollection
            .AddScoped<ServerService>()
            .AddScoped<UserService>()
            .AddScoped<NoteService>()
            .AddLogging(c => c.AddProvider(NullLoggerProvider.Instance));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceProvider serviceProvider)
    {
        var coreDatabase = serviceProvider.GetRequiredService<CoreDatabaseContext>();
        coreDatabase.Database.Create();

        var moderationDatabase = serviceProvider.GetRequiredService<ModerationDatabaseContext>();
        moderationDatabase.Database.Create();

        this.Database = moderationDatabase;
        this.Notes = serviceProvider.GetRequiredService<NoteService>();
    }

    /// <inheritdoc />
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
