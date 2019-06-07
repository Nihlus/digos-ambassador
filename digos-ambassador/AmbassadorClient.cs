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
using System.Threading.Tasks;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Behaviours;
using DIGOS.Ambassador.Services.Interactivity;
using DIGOS.Ambassador.Services.Servers;
using DIGOS.Ambassador.Services.Users;
using DIGOS.Ambassador.Transformations;
using DIGOS.Ambassador.TypeReaders;
using DIGOS.Ambassador.Utility;

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using log4net;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
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

        private readonly ContentService _content;

        private readonly CommandService _commands;

        private readonly IServiceProvider _services;

        private readonly BehaviourService _behaviours;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorClient"/> class.
        /// </summary>
        /// <param name="content">The content service.</param>
        public AmbassadorClient([NotNull] ContentService content)
        {
            this._client = Type.GetType("Mono.Runtime") is null
                ? new DiscordSocketClient()
                : new DiscordSocketClient(new DiscordSocketConfig { WebSocketProvider = () => new WebSocketSharpProvider() });

            this._client.Log += OnDiscordLogEvent;

            this._commands = new CommandService();
            this._commands.Log += OnDiscordLogEvent;

            this._content = content;
            this._commands = new CommandService();

            this._behaviours = new BehaviourService();

            this._services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(this._client)
                .AddSingleton<BaseSocketClient>(this._client)
                .AddSingleton(this._behaviours)
                .AddSingleton(this._content)
                .AddSingleton(this._commands)
                .AddSingleton<RoleplayService>()
                .AddSingleton<DiscordService>()
                .AddSingleton<CharacterService>()
                .AddSingleton<UserFeedbackService>()
                .AddSingleton<DossierService>()
                .AddSingleton<InteractivityService>()
                .AddSingleton<TransformationService>()
                .AddSingleton<LuaService>()
                .AddSingleton<KinkService>()
                .AddSingleton<PermissionService>()
                .AddSingleton<PrivacyService>()
                .AddSingleton<HelpService>()
                .AddSingleton<ServerService>()
                .AddSingleton<OwnedEntityService>()
                .AddSingleton<Random>()
                .AddDbContext<GlobalInfoContext>(builder => GlobalInfoContext.ConfigureOptions(builder))
                .BuildServiceProvider();

            var transformationService = this._services.GetRequiredService<TransformationService>();
            transformationService.WithDescriptionBuilder
            (
                ActivatorUtilities.CreateInstance<TransformationDescriptionBuilder>(this._services)
            );

            var characterService = this._services.GetRequiredService<CharacterService>();
            characterService.DiscoverPronounProviders();
        }

        /// <summary>
        /// Logs the ambassador into Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task LoginAsync()
        {
            await this._client.LoginAsync(TokenType.Bot, this._content.BotToken.Trim());
        }

        /// <summary>
        /// Starts the ambassador, allowing it to react to messages.
        /// </summary>
        /// <returns>A task representing the start action.</returns>
        public async Task StartAsync()
        {
            var db = this._services.GetRequiredService<GlobalInfoContext>();
            if (!((RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>()).Exists())
            {
                Log.Error("The database doesn't exist.");
                return;
            }

            this._commands.AddTypeReader<IMessage>(new UncachedMessageTypeReader<IMessage>());
            this._commands.AddTypeReader<Character>(new CharacterTypeReader());
            this._commands.AddTypeReader<Roleplay>(new RoleplayTypeReader());
            this._commands.AddTypeReader<Colour>(new ColourTypeReader());

            this._commands.AddEnumReader<UserClass>();
            this._commands.AddEnumReader<KinkPreference>();
            this._commands.AddEnumReader<Bodypart>();
            this._commands.AddEnumReader<Pattern>();
            this._commands.AddEnumReader<Permission>();
            this._commands.AddEnumReader<Permissions.PermissionTarget>();

            // Load modules and behaviours from the assembly this type was declared in
            var localAssembly = GetType().Assembly;
            await this._commands.AddModulesAsync(localAssembly, this._services);
            await this._behaviours.AddBehavioursAsync(localAssembly, this._services);

            await this._client.StartAsync();
            await this._behaviours.StartBehavioursAsync();
        }

        /// <summary>
        /// Logs the ambassador out of Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task LogoutAsync()
        {
            await this._client.LogoutAsync();
        }

        /// <summary>
        /// Stops the ambassador, releasing its Discord resources.
        /// </summary>
        /// <returns>A task representing the stop action.</returns>
        public async Task StopAsync()
        {
            await this._behaviours.StopBehavioursAsync();

            await LogoutAsync();
            await this._client.StopAsync();
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
