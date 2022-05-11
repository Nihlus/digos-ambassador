//
//  CharactersPlugin.cs
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Discord.Interactivity.Extensions;
using DIGOS.Ambassador.Plugins.Characters;
using DIGOS.Ambassador.Plugins.Characters.Autocomplete;
using DIGOS.Ambassador.Plugins.Characters.CommandModules;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Parsers;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Interfaces;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Discord.Commands.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: InternalsVisibleTo("DIGOS.Ambassador.Tests.Plugins.Characters")]
[assembly: RemoraPlugin(typeof(CharactersPlugin))]

namespace DIGOS.Ambassador.Plugins.Characters;

/// <summary>
/// Describes the character plugin.
/// </summary>
public sealed class CharactersPlugin : PluginDescriptor, IMigratablePlugin
{
    /// <inheritdoc />
    public override string Name => "Characters";

    /// <inheritdoc />
    public override string Description => "Provides user-managed character libraries.";

    /// <inheritdoc />
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        // Dependencies
        serviceCollection.AddInteractivity();

        // Our stuff
        serviceCollection.TryAddSingleton<PronounService>();
        serviceCollection.TryAddScoped<CharacterService>();
        serviceCollection.TryAddScoped<ICharacterService>(s => s.GetRequiredService<CharacterService>());
        serviceCollection.TryAddScoped<ICharacterEditor>(s => s.GetRequiredService<CharacterService>());
        serviceCollection.TryAddScoped<CharacterDiscordService>();
        serviceCollection.TryAddScoped<CharacterRoleService>();

        serviceCollection.AddConfiguredSchemaAwareDbContextPool<CharactersDatabaseContext>();

        serviceCollection.AddParser<CharacterParser>();
        serviceCollection
            .AddCommandTree()
                .WithCommandGroup<CharacterCommands>();

        serviceCollection.AddCondition<RequireEntityOwnerCondition<Character>>();

        serviceCollection.AddAutocompleteProvider<AnyCharacterAutocompleteProvider>();
        serviceCollection.AddAutocompleteProvider<OwnedCharacterAutocompleteProvider>();

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public override ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var permissionRegistry = serviceProvider.GetRequiredService<PermissionRegistryService>();
        var registrationResult = permissionRegistry.RegisterPermissions
        (
            Assembly.GetExecutingAssembly(),
            serviceProvider
        );

        if (!registrationResult.IsSuccess)
        {
            return new ValueTask<Result>(registrationResult);
        }

        var pronounService = serviceProvider.GetRequiredService<PronounService>();
        pronounService.DiscoverPronounProviders();

        return new ValueTask<Result>(Result.FromSuccess());
    }

    /// <inheritdoc />
    public async Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var context = serviceProvider.GetRequiredService<CharactersDatabaseContext>();

        await context.Database.MigrateAsync(ct);

        return Result.FromSuccess();
    }
}
