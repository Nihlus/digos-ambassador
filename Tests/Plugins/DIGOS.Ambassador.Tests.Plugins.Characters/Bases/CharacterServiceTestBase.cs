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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Tests.TestBases;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Remora.Rest.Core;
using Xunit;

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters;

/// <summary>
/// Serves as a test base for character service tests.
/// </summary>
public abstract class CharacterServiceTestBase : DatabaseProvidingTestBase, IAsyncLifetime
{
    /// <summary>
    /// Gets the default character owner.
    /// </summary>
    protected User DefaultOwner { get; private set; } = null!;

    /// <summary>
    /// Gets the default server characters are on.
    /// </summary>
    protected Server DefaultServer { get; private set; } = null!;

    /// <summary>
    /// Gets the database.
    /// </summary>
    protected CharactersDatabaseContext Database { get; private set; } = null!;

    /// <summary>
    /// Gets the character service object.
    /// </summary>
    protected ICharacterService Characters { get; private set; } = null!;

    /// <summary>
    /// Gets the character service object.
    /// </summary>
    protected ICharacterEditor CharacterEditor { get; private set; } = null!;

    /// <summary>
    /// Gets the user service.
    /// </summary>
    protected UserService Users { get; private set; } = null!;

    /// <summary>
    /// Gets the server service.
    /// </summary>
    protected ServerService Servers { get; private set; } = null!;

    /// <summary>
    /// Creates a character in the database with the given settings.
    /// </summary>
    /// <param name="owner">The owner. Defaults to <see cref="DefaultOwner"/>.</param>
    /// <param name="server">The server. Defaults <see cref="DefaultServer"/>.</param>
    /// <param name="name">The name.</param>
    /// <param name="avatarUrl">The avatar.</param>
    /// <param name="nickname">The nickname.</param>
    /// <param name="summary">The summary.</param>
    /// <param name="description">The description.</param>
    /// <param name="pronouns">The pronouns.</param>
    /// <param name="isNSFW">Whether the character is NSFW.</param>
    /// <returns>The character.</returns>
    protected Character CreateCharacter
    (
        User? owner = null,
        Server? server = null,
        string? name = null,
        string? avatarUrl = null,
        string? nickname = null,
        string? summary = null,
        string? description = null,
        string? pronouns = null,
        bool? isNSFW = null
    )
    {
        var character = this.Database.CreateProxy<Character>
        (
            owner ?? this.DefaultOwner,
            server ?? this.DefaultServer,
            name ?? string.Empty,
            avatarUrl ?? string.Empty,
            nickname ?? string.Empty,
            summary ?? string.Empty,
            description ?? string.Empty,
            pronouns ?? "They" // a real value is used here to avoid having to set it in the majority of cases
        );

        if (isNSFW is not null)
        {
            character.IsNSFW = isNSFW.Value;
        }

        this.Database.Characters.Update(character);
        this.Database.SaveChanges();

        return character;
    }

    /// <inheritdoc />
    protected override void RegisterServices(IServiceCollection serviceCollection)
    {
        serviceCollection
            .AddDbContext<CoreDatabaseContext>(o => ConfigureOptions(o, "Core"))
            .AddDbContext<CharactersDatabaseContext>(o => ConfigureOptions(o, "Characters"));

        serviceCollection
            .AddSingleton(FileSystemFactory.CreateContentFileSystem())
            .AddSingleton<PronounService>()
            .AddScoped<ContentService>()
            .AddScoped<ServerService>()
            .AddScoped<UserService>()
            .AddScoped<OwnedEntityService>()
            .AddScoped<CharacterService>()
            .AddScoped<ICharacterService>(s => s.GetRequiredService<CharacterService>())
            .AddScoped<ICharacterEditor>(s => s.GetRequiredService<CharacterService>())
            .AddLogging(c => c.AddProvider(NullLoggerProvider.Instance));
    }

    /// <inheritdoc />
    protected override void ConfigureServices(IServiceProvider serviceProvider)
    {
        var coreDatabase = serviceProvider.GetRequiredService<CoreDatabaseContext>();
        coreDatabase.Database.Create();

        var charactersDatabase = serviceProvider.GetRequiredService<CharactersDatabaseContext>();
        charactersDatabase.Database.Create();

        this.Database = charactersDatabase;

        this.Characters = serviceProvider.GetRequiredService<ICharacterService>();
        this.CharacterEditor = serviceProvider.GetRequiredService<ICharacterEditor>();

        this.Users = serviceProvider.GetRequiredService<UserService>();

        this.DefaultOwner = this.Database.CreateProxy<User>(new Snowflake(0));
        this.Database.Update(this.DefaultOwner);

        this.DefaultServer = this.Database.CreateProxy<Server>(new Snowflake(1));
        this.Database.Update(this.DefaultServer);

        this.Database.SaveChanges();

        // Default pronouns
        var pronounService = serviceProvider.GetRequiredService<PronounService>();
        pronounService.WithPronounProvider(new TheyPronounProvider());
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
