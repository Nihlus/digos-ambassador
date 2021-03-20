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
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Responders;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Behaviours.Services;
using Remora.Commands.Extensions;
using Remora.Commands.Trees.Nodes;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Caching.Extensions;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Parsers;
using Remora.Discord.Commands.Results;
using Remora.Discord.Commands.Services;
using Remora.Discord.Core;
using Remora.Discord.Gateway.Extensions;
using Remora.Discord.Hosting.Extensions;
using Remora.Plugins.Abstractions;
using Remora.Plugins.Services;

namespace DIGOS.Ambassador
{
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

            var pluginService = new PluginService();
            var plugins = pluginService.LoadAvailablePlugins().ToList();

            var hostBuilder = Host.CreateDefaultBuilder()
                .AddDiscordService(_ => token)
                .UseSystemd()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<BehaviourService>();

                    services
                        .AddSingleton(pluginService)
                        .AddSingleton(contentService)
                        .AddSingleton(contentFileSystem)
                        .AddSingleton<Random>();

                    services
                        .AddCommands()
                        .AddDiscordCaching();

                    services.Configure<AmbassadorCommandResponderOptions>(o => o.Prefix = "!");

                    // Custom responders & command services
                    services.AddCondition<RequireContextCondition>();
                    services.AddCondition<RequireOwnerCondition>();
                    services.AddCondition<RequireUserGuildPermissionCondition>();

                    services
                        .AddParser<IChannel, ChannelParser>()
                        .AddParser<IGuildMember, GuildMemberParser>()
                        .AddParser<IRole, RoleParser>()
                        .AddParser<IUser, UserParser>()
                        .AddParser<Snowflake, SnowflakeParser>();

                    services.TryAddScoped<ExecutionEventCollectorService>();
                    services.TryAddSingleton<SlashService>();

                    services.AddResponder<AmbassadorCommandResponder>();
                    services.AddResponder<AmbassadorInteractionResponder>();

                    foreach (var plugin in plugins)
                    {
                        plugin.ConfigureServices(services);
                    }
                })
                .ConfigureLogging(l =>
                {
                    l.ClearProviders();

                    l.AddLog4Net()
                        .AddFilter("Microsoft.EntityFrameworkCore.Infrastructure", LogLevel.Warning)
                        .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning)
                        .AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.Warning);
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
                var error = checkSlashSupport.Unwrap();
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
                var updateSlash = await slashService.UpdateSlashCommandsAsync(debugServer, cancellationSource.Token);
                if (!updateSlash.IsSuccess)
                {
                    log.LogWarning("Failed to update slash commands: {Reason}", updateSlash.Unwrap().Message);
                }
            }

            foreach (var plugin in plugins)
            {
                log.LogInformation("Initializing plugin {Name}, version {Version}...", plugin.Name, plugin.Version);
                var initializePlugin = await plugin.InitializeAsync(hostServices);
                if (!initializePlugin.IsSuccess)
                {
                    log.LogError
                    (
                        "Failed to initialize plugin {Name}: {Error}",
                        plugin.Name,
                        initializePlugin.Unwrap().Message
                    );

                    return;
                }

                if (plugin is not IMigratablePlugin migratablePlugin)
                {
                    continue;
                }

                log.LogInformation("Applying plugin migrations...");

                var migratePlugin = await migratablePlugin.MigratePluginAsync(hostServices);
                if (migratePlugin.IsSuccess)
                {
                    continue;
                }

                log.LogError
                (
                    "Failed to migrate plugin {Name}: {Error}",
                    plugin.Name,
                    migratePlugin.Unwrap().Message
                );

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
}
