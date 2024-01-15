//
//  Program.cs
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
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.ExecutionEventServices;
using DIGOS.Ambassador.Responders;
using DIGOS.Ambassador.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Commands.Extensions;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Caching.Services;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Themes;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Plugins.Services;
using Remora.Rest.Core;
using Remora.Results;
using Serilog;

namespace DIGOS.Ambassador;

/// <summary>
/// The main entry point class of the program.
/// </summary>
internal class Program
{
    /// <summary>
    /// The main entry point of the program.
    /// </summary>
    /// <returns>A task.</returns>
    public static async Task Main()
    {
        var cancellationSource = new CancellationTokenSource();

        Console.CancelKeyPress += (_, eventArgs) =>
        {
            eventArgs.Cancel = true;
            cancellationSource.Cancel();
        };

        var contentFileSystem = FileSystemFactory.CreateContentFileSystem();
        var contentService = new ContentService(contentFileSystem);

        var options = Options.Create(new PluginServiceOptions(Array.Empty<string>()));
        var pluginService = new PluginService(options);

        var plugins = pluginService.LoadPluginTree();

        var hostBuilder = Host.CreateApplicationBuilder();

        hostBuilder.Configuration
            .AddAmbassadorConfigurationFiles();

        hostBuilder.Services
            .AddDiscordService
            (
                _ => hostBuilder.Configuration.GetValue<string>("Discord:Token")
                     ?? throw new InvalidOperationException("No bot token set")
            );

        hostBuilder.Services.AddSystemd();

        hostBuilder.Services.Configure<DiscordGatewayClientOptions>(o =>
        {
            o.Intents |= GatewayIntents.MessageContents;
        });

        hostBuilder.Services.Configure<ServiceProviderOptions>(s =>
        {
            s.ValidateScopes = true;
            s.ValidateOnBuild = true;
        });

        hostBuilder.Services.Configure<CommandResponderOptions>(o => o.Prefix = "!");

        hostBuilder.Services
            .AddSingleton(pluginService)
            .AddSingleton(contentService)
            .AddSingleton(contentFileSystem)
            .AddScoped<MessageRelayService>()
            .AddSingleton<Random>();

        hostBuilder.Services
            .AddDiscordCommands(true)
            .AddDiscordCaching();

        // Configure cache times
        hostBuilder.Services.Configure<CacheSettings>(settings =>
        {
            settings.SetAbsoluteExpiration<IGuildMember>(TimeSpan.FromDays(1));
            settings.SetAbsoluteExpiration<IMessage>(TimeSpan.FromDays(1));
        });

        // Set up the feedback theme
        var theme = (FeedbackTheme)FeedbackTheme.DiscordDark with
        {
            Secondary = Color.MediumPurple
        };

        hostBuilder.Services.AddSingleton<IFeedbackTheme>(theme);

        // Add execution events
        hostBuilder.Services
            .AddPreExecutionEvent<ConsentCheckingPreExecutionEvent>()
            .AddPreparationErrorEvent<MessageRelayingPreparationErrorEvent>()
            .AddPostExecutionEvent<MessageRelayingPostExecutionEvent>();

        // Ensure we're automatically joining created threads
        hostBuilder.Services.AddResponder<ThreadJoinResponder>();

        // Override the default responders
        hostBuilder.Services.Replace(ServiceDescriptor.Scoped<CommandResponder, AmbassadorCommandResponder>());
        hostBuilder.Services.Replace(ServiceDescriptor.Scoped<InteractionResponder, AmbassadorInteractionResponder>());

        hostBuilder.Services.AddParser<HumanTimeSpanReader>();

        var configurePlugins = plugins.ConfigureServices(hostBuilder.Services);
        if (!configurePlugins.IsSuccess)
        {
            throw new InvalidOperationException();
        }

        var logger = new LoggerConfiguration();
        logger.ReadFrom.Configuration(hostBuilder.Configuration);

        hostBuilder.Logging
            .ClearProviders()
            .AddSerilog(logger.CreateLogger());

        var host = hostBuilder.Build();
        var hostServices = host.Services;

        var log = hostServices.GetRequiredService<ILogger<Program>>();
        log.LogInformation("Running on {Framework}", RuntimeInformation.FrameworkDescription);

        Snowflake? debugServer = null;

        var debugServerString = hostBuilder.Configuration.GetValue<string>("REMORA_DEBUG_SERVER")
                                ?? hostBuilder.Configuration.GetValue<string>("Remora:DebugServer");

        if (debugServerString is not null)
        {
            if (!Snowflake.TryParse(debugServerString, out debugServer))
            {
                log.LogWarning("Failed to parse debug server from environment");
            }
        }

        var slashService = hostServices.GetRequiredService<SlashService>();

        var updateSlash = await slashService.UpdateSlashCommandsAsync(debugServer, ct: cancellationSource.Token);
        if (!updateSlash.IsSuccess)
        {
            log.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error.Message);
        }

        log.LogInformation("Migrating plugins...");
        var migratePlugins = await plugins.MigrateAsync(hostServices, cancellationSource.Token);
        if (!migratePlugins.IsSuccess)
        {
            log.LogError("Failed to initialize the plugin tree");

            if (migratePlugins.Error is AggregateError a)
            {
                foreach (var error in a.Errors)
                {
                    if (error.IsSuccess)
                    {
                        continue;
                    }

                    log.LogError("Migration error: {Error}", error.Error!.Message);
                }
            }
            else
            {
                log.LogError("Migration error: {Error}", migratePlugins.Error);
            }

            return;
        }

        log.LogInformation("Initializing plugins...");
        var initializePlugins = await plugins.InitializeAsync(hostServices, cancellationSource.Token);
        if (!initializePlugins.IsSuccess)
        {
            log.LogError("Failed to initialize the plugin tree");

            if (initializePlugins.Error is AggregateError a)
            {
                foreach (var error in a.Errors)
                {
                    if (error.IsSuccess)
                    {
                        continue;
                    }

                    log.LogError("Initialization error: {Error}", error.Error!.Message);
                }
            }
            else
            {
                log.LogError("Initialization error: {Error}", initializePlugins.Error);
            }

            return;
        }

        await host.RunAsync(cancellationSource.Token);
    }
}
