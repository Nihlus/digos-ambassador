﻿//
//  OwnedEntityServiceTestBase.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Tests.TestBases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Core;

/// <summary>
/// Serves as a test base for owned entity service tests.
/// </summary>
[PublicAPI]
public abstract class OwnedEntityServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
{
    /// <summary>
    /// Gets the database.
    /// </summary>
    protected CoreDatabaseContext Database { get; private set; } = null!;

    /// <summary>
    /// Gets the owned entity service object.
    /// </summary>
    protected OwnedEntityService Entities { get; private set; } = null!;

    /// <summary>
    /// Gets the user service.
    /// </summary>
    protected UserService Users { get; private set; } = null!;

    /// <inheritdoc />
    protected override void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddDbContext<CoreDatabaseContext>(o => ConfigureOptions(o, "Core"));

        serviceCollection
            .AddScoped<OwnedEntityService>()
            .AddScoped<UserService>()
            .AddLogging(c => c.AddProvider(NullLoggerProvider.Instance));
    }

    /// <inheritdoc />
    protected sealed override void ConfigureServices(IServiceProvider serviceProvider)
    {
        this.Database = serviceProvider.GetRequiredService<CoreDatabaseContext>();
        this.Database.Database.Create();

        this.Entities = serviceProvider.GetRequiredService<OwnedEntityService>();
        this.Users = serviceProvider.GetRequiredService<UserService>();
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
