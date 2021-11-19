//
//  ModerationPlugin.cs
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Extensions;
using DIGOS.Ambassador.Discord.Interactivity.Extensions;
using DIGOS.Ambassador.Discord.Pagination.Responders;
using DIGOS.Ambassador.Plugins.Moderation;
using DIGOS.Ambassador.Plugins.Moderation.Behaviours;
using DIGOS.Ambassador.Plugins.Moderation.CommandModules;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Plugins.Moderation.Responders;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Remora.Behaviours.Extensions;
using Remora.Commands.Extensions;
using Remora.Discord.Gateway.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Abstractions.Attributes;
using Remora.Results;

[assembly: RemoraPlugin(typeof(ModerationPlugin))]

namespace DIGOS.Ambassador.Plugins.Moderation;

/// <summary>
/// Describes the moderation plugin.
/// </summary>
public class ModerationPlugin : PluginDescriptor, IMigratablePlugin
{
    /// <inheritdoc />
    public override string Name => "Moderation";

    /// <inheritdoc />
    public override string Description => "Provides simple moderation tools.";

    /// <inheritdoc />
    public override Result ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.TryAddInteractivityResponder<PaginatedMessageResponder>();
        serviceCollection
            .AddConfiguredSchemaAwareDbContextPool<ModerationDatabaseContext>();

        serviceCollection
            .AddScoped<ModerationService>()
            .AddScoped<NoteService>()
            .AddScoped<WarningService>()
            .AddScoped<BanService>()
            .AddScoped<ChannelLoggingService>();

        serviceCollection
            .AddCommandGroup<BanCommands>()
            .AddCommandGroup<ModerationCommands>()
            .AddCommandGroup<NoteCommands>()
            .AddCommandGroup<WarningCommands>();

        serviceCollection.AddResponder<EventLoggingResponder>();
        serviceCollection.AddBehaviour<ExpirationBehaviour>();

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
        var context = serviceProvider.GetRequiredService<ModerationDatabaseContext>();

        await context.Database.MigrateAsync(ct);

        return Result.FromSuccess();
    }
}
