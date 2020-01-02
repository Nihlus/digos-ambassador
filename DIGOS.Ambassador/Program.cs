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
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Xml;
using DIGOS.Ambassador.Core.Database.Services;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Behaviours.Services;
using Remora.EntityFrameworkCore.Modular.Services;
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
            // Configure logging
            const string configurationName = "DIGOS.Ambassador.log4net.config";
            var logConfig = new XmlDocument();
            using (var configStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(configurationName))
            {
                if (configStream is null)
                {
                    throw new InvalidOperationException("The log4net configuration stream could not be found.");
                }

                logConfig.Load(configStream);
            }

            var repo = LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(Hierarchy));
            XmlConfigurator.Configure(repo, logConfig["log4net"]);

            var hostBuilder = Host.CreateDefaultBuilder()
                .UseSystemd()
                .ConfigureServices(services =>
                {
                    var pluginService = new PluginService();

                    services
                        .AddHostedService<AmbassadorBotService>()
                        .AddSingleton(pluginService)
                        .AddSingleton
                        (
                            provider => new DiscordSocketClient(new DiscordSocketConfig { MessageCacheSize = 100 })
                        )
                        .AddSingleton<IDiscordClient>(s => s.GetRequiredService<DiscordSocketClient>())
                        .AddSingleton<BaseSocketClient>(s => s.GetRequiredService<DiscordSocketClient>())
                        .AddSingleton<CommandService>()
                        .AddSingleton<BehaviourService>()
                        .AddSingleton<ContentService>()
                        .AddSingleton<DiscordService>()
                        .AddSingleton<UserFeedbackService>()
                        .AddSingleton<InteractivityService>()
                        .AddSingleton<DelayedActionService>()
                        .AddSingleton<SchemaAwareDbContextService>()
                        .AddSingleton<ContextConfigurationService>()
                        .AddSingleton(FileSystemFactory.CreateContentFileSystem())
                        .AddSingleton<Random>();

                    var plugins = pluginService.LoadAvailablePlugins();
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
                        .AddFilter("Microsoft.EntityFrameworkCore.Query", LogLevel.Error)
                        .AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning)
                        .AddFilter("Microsoft.EntityFrameworkCore.Migrations", LogLevel.Warning);
                });

            var host = hostBuilder.Build();

            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation($"Running on {RuntimeInformation.FrameworkDescription}");

            await host.RunAsync();
        }
    }
}
