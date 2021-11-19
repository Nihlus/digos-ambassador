//
//  PronounServiceTestBase.cs
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
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Tests.TestBases;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters;

/// <summary>
/// Serves as a test base for pronoun service tests.
/// </summary>
[PublicAPI]
public abstract class PronounServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
{
    /// <summary>
    /// Gets the pronoun service object.
    /// </summary>
    protected PronounService Pronouns { get; private set; } = null!;

    /// <inheritdoc />
    protected override void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddSingleton<PronounService>();
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceProvider serviceProvider)
    {
        this.Pronouns = serviceProvider.GetRequiredService<PronounService>();
    }

    /// <inheritdoc />
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }
}
