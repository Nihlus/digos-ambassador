//
//  AutorolePlugin.cs
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
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Plugins.Autorole;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Parsers;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Remora.Commands.Extensions;
using Remora.Discord.Pagination.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;
using AutoroleCommands = DIGOS.Ambassador.Plugins.Autorole.CommandModules.AutoroleCommands;

[assembly: InternalsVisibleTo("DIGOS.Ambassador.Tests.Plugins.Autorole")]
[assembly: RemoraPlugin(typeof(AutorolePlugin))]

namespace DIGOS.Ambassador.Plugins.Autorole;

/// <summary>
/// Describes the character plugin.
/// </summary>
public sealed class AutorolePlugin : PluginDescriptor, IMigratablePlugin
{
    /// <inheritdoc />
    public override string Name => "Autorole";

    /// <inheritdoc />
    public override string Description => "Allows administrators to create automated role assignments.";

    /// <inheritdoc />
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddPagination();

        serviceCollection.TryAddScoped<AutoroleService>();
        serviceCollection.TryAddScoped<AutoroleUpdateService>();
        serviceCollection.TryAddScoped<UserStatisticsService>();

        serviceCollection.AddParser<AutoroleConfigurationParser>();
        serviceCollection.AddCommandTree()
            .WithCommandGroup<AutoroleCommands>();

        serviceCollection.AddConfiguredSchemaAwareDbContextPool<AutoroleDatabaseContext>();

        /*
        serviceCollection.AddResponder<MessageCountConditionResponder>();
        serviceCollection.AddResponder<ReactionConditionResponder>();
        serviceCollection.AddResponder<UserActivityResponder>();
        serviceCollection.AddResponder<RoleConditionResponder>();

        serviceCollection.Configure<DiscordGatewayClientOptions>(o => o.Intents |= GatewayIntents.GuildPresences);
        */

        return Result.FromSuccess();
    }

    /// <inheritdoc />
    public override ValueTask<Result> InitializeAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        var permissionRegistry = serviceProvider.GetRequiredService<PermissionRegistryService>();
        return new ValueTask<Result>(permissionRegistry.RegisterPermissions
        (
            Assembly.GetExecutingAssembly(),
            serviceProvider
        ));
    }

    /// <inheritdoc />
    public async Task<Result> MigrateAsync(IServiceProvider serviceProvider, CancellationToken ct = default)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AutoroleDatabaseContext>();

        await context.Database.MigrateAsync(ct);

        return Result.FromSuccess();
    }
}
