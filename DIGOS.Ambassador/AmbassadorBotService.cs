//
//  AmbassadorBotService.cs
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Interactivity.Behaviours;
using DIGOS.Ambassador.Plugins.Abstractions.Database;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Remora.Behaviours;
using Remora.Behaviours.Services;
using Remora.Plugins.Services;
using Remora.Results;

#pragma warning disable SA1118 // Parameter spans multiple lines, big strings

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador
{
    /// <summary>
    /// Main service for the bot itself. Handles high-level functionality.
    /// </summary>
    public class AmbassadorBotService : HostedBotService<AmbassadorBotService>
    {
        private readonly DiscordSocketClient _client;
        private readonly BehaviourService _behaviours;
        private readonly PluginService _pluginService;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorBotService"/> class.
        /// </summary>
        /// <param name="discordClient">The Discord client.</param>
        /// <param name="commandService">The command service.</param>
        /// <param name="behaviourService">The behaviour service.</param>
        /// <param name="pluginService">The plugin service.</param>
        /// <param name="hostConfiguration">The host configuration.</param>
        /// <param name="hostEnvironment">The host environment.</param>
        /// <param name="log">The logging instance.</param>
        /// <param name="applicationLifetime">The application lifetime.</param>
        /// <param name="services">The available services.</param>
        public AmbassadorBotService
        (
            DiscordSocketClient discordClient,
            CommandService commandService,
            BehaviourService behaviourService,
            PluginService pluginService,
            IConfiguration hostConfiguration,
            IHostEnvironment hostEnvironment,
            ILogger<AmbassadorBotService> log,
            IHostApplicationLifetime applicationLifetime,
            IServiceProvider services
        )
            : base(hostConfiguration, hostEnvironment, log, applicationLifetime, services)
        {
            _client = discordClient;
            _behaviours = behaviourService;
            _pluginService = pluginService;

            _client.Log += OnDiscordLogEvent;
            commandService.Log += OnDiscordLogEvent;
        }

        /// <inheritdoc />
        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!await InitializePluginsAsync())
            {
                this.Log.LogError("Failed to initialize the available plugins.");

                // Plugin failures means we won't continue
                this.Lifetime.StopApplication();

                return;
            }

            var loginResult = await LoginAsync();
            if (!loginResult.IsSuccess)
            {
                this.Log.LogError(loginResult.Exception, loginResult.ErrorReason);

                // Login failures means we won't continue
                this.Lifetime.StopApplication();

                return;
            }

            await _behaviours.AddBehaviourAsync<InteractivityBehaviour>(this.Services);
            await _behaviours.AddBehaviourAsync<DelayedActionBehaviour>(this.Services);

            await _client.StartAsync();
            await _behaviours.StartBehavioursAsync();
        }

        /// <inheritdoc />
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            this.Log.LogInformation("Stopping behaviours...");
            await _behaviours.StopBehavioursAsync();

            await _client.LogoutAsync();
            await _client.StopAsync();
        }

        private async Task<bool> InitializePluginsAsync()
        {
            var plugins = _pluginService.LoadAvailablePlugins().ToList();

            // Create plugin databases
            foreach (var plugin in plugins)
            {
                if (!(plugin is IMigratablePlugin migratablePlugin))
                {
                    continue;
                }

                if (await migratablePlugin.IsDatabaseCreatedAsync(this.Services))
                {
                    continue;
                }

                if (await migratablePlugin.MigratePluginAsync(this.Services))
                {
                    continue;
                }

                this.Log.LogWarning
                (
                    $"The plugin \"{plugin.Name}\" (v{plugin.Version}) failed to create its database."
                );

                return false;
            }

            // Then, run migrations in reverse
            foreach (var plugin in plugins.AsEnumerable().Reverse())
            {
                if (!(plugin is IMigratablePlugin migratablePlugin))
                {
                    continue;
                }

                if (await migratablePlugin.MigratePluginAsync(this.Services))
                {
                    continue;
                }

                this.Log.LogWarning
                (
                    $"The plugin \"{plugin.Name}\" (v{plugin.Version}) failed to migrate its database."
                );

                return false;
            }

            foreach (var plugin in plugins)
            {
                if (await plugin.InitializeAsync(this.Services))
                {
                    continue;
                }

                this.Log.LogWarning
                (
                    $"The plugin \"{plugin.Name}\" (v{plugin.Version}) failed to initialize."
                );

                return false;
            }

            return true;
        }

        /// <summary>
        /// Logs the ambassador into Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        private async Task<ModifyEntityResult> LoginAsync()
        {
            var contentService = this.Services.GetRequiredService<ContentService>();

            var getTokenResult = await contentService.GetBotTokenAsync();
            if (!getTokenResult.IsSuccess)
            {
                return ModifyEntityResult.FromError(getTokenResult);
            }

            var token = getTokenResult.Entity.Trim();

            await _client.LoginAsync(TokenType.Bot, token);

            return ModifyEntityResult.FromSuccess();
        }

        /// <summary>
        /// Saves log events from Discord using the configured method in log4net.
        /// </summary>
        /// <param name="arg">The log message from Discord.</param>
        /// <returns>A completed task.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the log severity is not recognized.</exception>
        [NotNull]
        private Task OnDiscordLogEvent(LogMessage arg)
        {
            var content = $"Discord log event: {arg.Message}";
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                {
                    this.Log.LogCritical(content, arg.Exception);
                    break;
                }
                case LogSeverity.Error:
                {
                    this.Log.LogError(content, arg.Exception);
                    break;
                }
                case LogSeverity.Warning:
                {
                    this.Log.LogWarning(content, arg.Exception);
                    break;
                }
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                {
                    this.Log.LogInformation(content, arg.Exception);
                    break;
                }
                case LogSeverity.Debug:
                {
                    this.Log.LogDebug(content, arg.Exception);
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return Task.CompletedTask;
        }
    }
}
