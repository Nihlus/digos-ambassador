//
//  Program.cs
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
using System.Drawing;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.ExecutionEventServices;
using DIGOS.Ambassador.Responders;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Remora.Behaviours.Services;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Gateway.Commands;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Caching.Services;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Themes;
using Remora.Discord.Commands.Responders;
using Remora.Discord.Commands.Results;
using Remora.Discord.Commands.Services;
using Remora.Discord.Gateway;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Plugins.Services;
using Remora.Rest.Core;
using Remora.Results;

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

        // Configure logging
        const string configurationName = "DIGOS.Ambassador.log4net.config";
        var logConfig = new XmlDocument();
        await using (var configStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(configurationName))
        {
            if (configStream is null)
            {
                throw new InvalidOperationException("The log4net configuration stream could not be found.");
            }

            logConfig.Load(configStream);
        }

        var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
        XmlConfigurator.Configure(repo, logConfig["log4net"]);

        var contentFileSystem = FileSystemFactory.CreateContentFileSystem();
        var contentService = new ContentService(contentFileSystem);

        var getBotToken = await contentService.GetBotTokenAsync();
        if (!getBotToken.IsSuccess)
        {
            throw new InvalidOperationException("No bot token available.");
        }

        var token = getBotToken.Entity.Trim();

        var options = Options.Create(new PluginServiceOptions(Array.Empty<string>()));
        var pluginService = new PluginService(options);

        var plugins = pluginService.LoadPluginTree();

        var hostBuilder = Host.CreateDefaultBuilder()
            .AddDiscordService(_ => token)
            .UseSystemd()
            .ConfigureServices(services =>
            {
                services.Configure<DiscordGatewayClientOptions>(o =>
                {
                    o.Intents |= GatewayIntents.MessageContents;
                });

                services.Configure<ServiceProviderOptions>(s =>
                {
                    s.ValidateScopes = true;
                    s.ValidateOnBuild = true;
                });

                services.Configure<CommandResponderOptions>(o => o.Prefix = "!");

                services.AddSingleton<BehaviourService>();

                services
                    .AddSingleton(pluginService)
                    .AddSingleton(contentService)
                    .AddSingleton(contentFileSystem)
                    .AddSingleton<Random>();

                services
                    .AddDiscordCommands(true)
                    .AddDiscordCaching();

                // Configure cache times
                services.Configure<CacheSettings>(settings =>
                {
                    settings.SetAbsoluteExpiration<IGuildMember>(TimeSpan.FromDays(1));
                    settings.SetAbsoluteExpiration<IMessage>(TimeSpan.FromDays(1));
                });

                // Set up the feedback theme
                var theme = (FeedbackTheme)FeedbackTheme.DiscordDark with
                {
                    Secondary = Color.MediumPurple
                };

                services.AddSingleton<IFeedbackTheme>(theme);

                // Add execution events
                services
                    .AddPreExecutionEvent<ConsentCheckingPreExecutionEvent>()
                    .AddPostExecutionEvent<MessageRelayingPostExecutionEvent>();

                // Ensure we're automatically joining created threads
                services.AddResponder<ThreadJoinResponder>();

                // Override the default responders
                services.Replace(ServiceDescriptor.Scoped<CommandResponder, AmbassadorCommandResponder>());
                services.Replace(ServiceDescriptor.Scoped<InteractionResponder, AmbassadorInteractionResponder>());

                var configurePlugins = plugins.ConfigureServices(services);
                if (!configurePlugins.IsSuccess)
                {
                    throw new InvalidOperationException();
                }
            })
            .ConfigureLogging(l =>
            {
                l.ClearProviders();

                l.AddLog4Net()
                    .AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning)
                    .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Critical)
                    .AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.Warning)
                    .AddFilter("Microsoft.EntityFrameworkCore.Update", LogLevel.Critical);
            });

        var host = hostBuilder.Build();
        var hostServices = host.Services;

        var log = hostServices.GetRequiredService<ILogger<Program>>();
        log.LogInformation("Running on {Framework}", RuntimeInformation.FrameworkDescription);

        Snowflake? debugServer = null;
        var debugServerString = Environment.GetEnvironmentVariable("REMORA_DEBUG_SERVER");
        if (debugServerString is not null)
        {
            if (!Snowflake.TryParse(debugServerString, out debugServer))
            {
                log.LogWarning("Failed to parse debug server from environment");
            }
        }

        var slashService = hostServices.GetRequiredService<SlashService>();

        var checkSlashSupport = slashService.SupportsSlashCommands();
        if (!checkSlashSupport.IsSuccess)
        {
            var error = checkSlashSupport.Error;
            if (error is UnsupportedFeatureError ufe)
            {
                var location = ufe.Node is not null
                    ? GetCommandLocation(ufe.Node)
                    : "unknown";

                log.LogWarning
                (
                    "The registered commands of the bot don't support slash commands: {Reason} ({Location})",
                    error.Message,
                    location
                );
            }
            else
            {
                log.LogError("Failed to check slash command compatibility: {Reason}", error.Message);
                return;
            }
        }
        else
        {
            var updateSlash = await slashService.UpdateSlashCommandsAsync(debugServer, ct: cancellationSource.Token);
            if (!updateSlash.IsSuccess)
            {
                log.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Error.Message);
            }
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

        var behaviourService = hostServices.GetRequiredService<BehaviourService>();
        await behaviourService.StartBehavioursAsync();

        await host.RunAsync(cancellationSource.Token);
        await behaviourService.StopBehavioursAsync();
    }

    private static string GetCommandLocation(IChildNode node)
    {
        var sb = new StringBuilder();

        switch (node)
        {
            case GroupNode group:
            {
                IParentNode current = group;
                while (current is IChildNode child)
                {
                    sb.Insert(0, "::");
                    sb.Insert(0, child.Key);
                    current = child.Parent;
                }
                break;
            }
            case CommandNode command:
            {
                sb.Append(command.GroupType.FullName);
                sb.Append("::");
                sb.Append(command.CommandMethod.Name);
                break;
            }
        }

        return sb.ToString();
    }
}
