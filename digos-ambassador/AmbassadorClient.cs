//
//  AmbassadorClient.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core;
using DIGOS.Ambassador.Core.Database.Services;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord;
using DIGOS.Ambassador.Discord.Behaviours.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Interactivity.Behaviours;
using DIGOS.Ambassador.Plugins.Abstractions;
using DIGOS.Ambassador.Plugins.Services;
using DIGOS.Ambassador.Services;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using log4net;
using Microsoft.Extensions.DependencyInjection;

#pragma warning disable SA1118 // Parameter spans multiple lines, big strings

namespace DIGOS.Ambassador
{
    /// <summary>
    /// Main class for the bot itself. Handles high-level functionality.
    /// </summary>
    public class AmbassadorClient
    {
        /// <summary>
        /// Logger instance for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(typeof(AmbassadorClient));

        private readonly DiscordSocketClient _client;

        private readonly CommandService _commands;

        private readonly BehaviourService _behaviours;

        private IServiceProvider _services;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorClient"/> class.
        /// </summary>
        public AmbassadorClient()
        {
            var config = new DiscordSocketConfig
            {
                MessageCacheSize = 100
            };

            _client = new DiscordSocketClient(config);

            _client.Log += OnDiscordLogEvent;

            _commands = new CommandService();
            _commands.Log += OnDiscordLogEvent;
            _commands = new CommandService();

            _behaviours = new BehaviourService();
        }

        /// <summary>
        /// Initializes the bot and its services.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task InitializeAsync()
        {
            var pluginService = new PluginService();

            var serviceCollection = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(_client)
                .AddSingleton<BaseSocketClient>(_client)
                .AddSingleton(_behaviours)
                .AddSingleton<ContentService>()
                .AddSingleton(_commands)
                .AddSingleton<DiscordService>()
                .AddSingleton<UserFeedbackService>()
                .AddSingleton<InteractivityService>()
                .AddSingleton<DelayedActionService>()
                .AddSingleton<HelpService>()
                .AddSingleton<Random>()
                .AddSingleton(pluginService)
                .AddSingleton<SchemaAwareDbContextService>()
                .AddSingleton(FileSystemFactory.CreateContentFileSystem());

            var successfullyRegisteredPlugins = new List<IPluginDescriptor>();

            var availablePlugins = pluginService.LoadAvailablePlugins();
            foreach (var availablePlugin in availablePlugins)
            {
                if (!await availablePlugin.RegisterServicesAsync(serviceCollection))
                {
                    Log.Warn
                    (
                        $"The plugin \"{availablePlugin.Name}\" (v{availablePlugin.Version}) failed to " +
                        $"register its services. It will not be loaded."
                    );

                    continue;
                }

                successfullyRegisteredPlugins.Add(availablePlugin);
            }

            _services = serviceCollection.BuildServiceProvider();

            var contentService = _services.GetRequiredService<ContentService>();
            await contentService.InitializeAsync();

            // Create plugin databases
            foreach (var plugin in successfullyRegisteredPlugins)
            {
                if (!(plugin is IMigratablePlugin migratablePlugin))
                {
                    continue;
                }

                if (await migratablePlugin.IsDatabaseCreatedAsync(_services))
                {
                    continue;
                }

                if (!await migratablePlugin.MigratePluginAsync(_services))
                {
                    Log.Warn
                    (
                        $"The plugin \"{plugin.Name}\"" +
                        $" (v{plugin.Version}) failed to migrate its database. It may not " +
                        $"be functional."
                    );
                }
            }

            // Then, run migrations in reverse
            foreach (var plugin in successfullyRegisteredPlugins.AsEnumerable().Reverse())
            {
                if (!(plugin is IMigratablePlugin migratablePlugin))
                {
                    continue;
                }

                if (!await migratablePlugin.MigratePluginAsync(_services))
                {
                    Log.Warn
                    (
                        $"The plugin \"{plugin.Name}\"" +
                        $" (v{plugin.Version}) failed to migrate its database. It may not " +
                        $"be functional."
                    );
                }
            }

            foreach (var plugin in successfullyRegisteredPlugins)
            {
                if (!await plugin.InitializeAsync(_services))
                {
                    Log.Warn
                    (
                        $"The plugin \"{plugin.Name}\"" +
                        $" (v{plugin.Version}) failed to initialize. It may not be functional."
                    );
                }
            }
        }

        /// <summary>
        /// Logs the ambassador into Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task<ModifyEntityResult> LoginAsync()
        {
            var contentService = _services.GetRequiredService<ContentService>();

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
        /// Starts the ambassador, allowing it to react to messages.
        /// </summary>
        /// <returns>A task representing the start action.</returns>
        public async Task StartAsync()
        {
            // Load modules and behaviours from the assembly this type was declared in
            var localAssembly = GetType().Assembly;
            await _commands.AddModulesAsync(localAssembly, _services);

            await _behaviours.AddBehavioursAsync(localAssembly, _services);
            await _behaviours.AddBehaviourAsync<InteractivityBehaviour>(_services);
            await _behaviours.AddBehaviourAsync<DelayedActionBehaviour>(_services);

            await _client.StartAsync();
            await _behaviours.StartBehavioursAsync();
        }

        /// <summary>
        /// Logs the ambassador out of Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task LogoutAsync()
        {
            await _client.LogoutAsync();
        }

        /// <summary>
        /// Stops the ambassador, releasing its Discord resources.
        /// </summary>
        /// <returns>A task representing the stop action.</returns>
        public async Task StopAsync()
        {
            await _behaviours.StopBehavioursAsync();

            await LogoutAsync();
            await _client.StopAsync();
        }

        /// <summary>
        /// Saves log events from Discord using the configured method in log4net.
        /// </summary>
        /// <param name="arg">The log message from Discord.</param>
        /// <returns>A completed task.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the log severity is not recognized.</exception>
        [NotNull]
        private static Task OnDiscordLogEvent(LogMessage arg)
        {
            var content = $"Discord log event: {arg.Message}";
            switch (arg.Severity)
            {
                case LogSeverity.Critical:
                {
                    Log.Fatal(content, arg.Exception);
                    break;
                }
                case LogSeverity.Error:
                {
                    Log.Error(content, arg.Exception);
                    break;
                }
                case LogSeverity.Warning:
                {
                    Log.Warn(content, arg.Exception);
                    break;
                }
                case LogSeverity.Verbose:
                case LogSeverity.Info:
                {
                    Log.Info(content, arg.Exception);
                    break;
                }
                case LogSeverity.Debug:
                {
                    Log.Debug(content, arg.Exception);
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
