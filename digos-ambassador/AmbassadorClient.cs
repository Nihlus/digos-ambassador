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
using System.Reflection;
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

        private readonly DiscordSocketClient Client;

        private readonly ContentService Content;

        private readonly CommandService Commands;

        private readonly IServiceProvider Services;

        private readonly BehaviourService Behaviours;

        /// <summary>
        /// Initializes a new instance of the <see cref="AmbassadorClient"/> class.
        /// </summary>
        /// <param name="content">The content service.</param>
        public AmbassadorClient([NotNull] ContentService content)
        {
            this.Client = Type.GetType("Mono.Runtime") is null
                ? new DiscordSocketClient()
                : new DiscordSocketClient(new DiscordSocketConfig { WebSocketProvider = () => new WebSocketSharpProvider() });

            this.Client.Log += OnDiscordLogEvent;

            this.Commands = new CommandService();
            this.Commands.Log += OnDiscordLogEvent;

            this.Content = content;
            this.Commands = new CommandService();

            this.Behaviours = new BehaviourService();

            this.Services = new ServiceCollection()
                .AddSingleton(this)
                .AddSingleton(this.Client)
                .AddSingleton<BaseSocketClient>(this.Client)
                .AddSingleton(this.Behaviours)
                .AddSingleton(this.Content)
                .AddSingleton(this.Commands)
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
                .AddDbContextPool<GlobalInfoContext>(builder => GlobalInfoContext.ConfigureOptions(builder))
                .BuildServiceProvider();

            var transformationService = this.Services.GetRequiredService<TransformationService>();
            transformationService.WithDescriptionBuilder
            (
                ActivatorUtilities.CreateInstance<TransformationDescriptionBuilder>(this.Services)
            );
        }

        /// <summary>
        /// Logs the ambassador into Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task LoginAsync()
        {
            await this.Client.LoginAsync(TokenType.Bot, this.Content.BotToken.Trim());
        }

        /// <summary>
        /// Starts the ambassador, allowing it to react to messages.
        /// </summary>
        /// <returns>A task representing the start action.</returns>
        public async Task StartAsync()
        {
            var db = this.Services.GetRequiredService<GlobalInfoContext>();
            if (!((RelationalDatabaseCreator)db.Database.GetService<IDatabaseCreator>()).Exists())
            {
                Log.Error("The database doesn't exist.");
                return;
            }

            this.Commands.AddTypeReader<IMessage>(new UncachedMessageTypeReader<IMessage>());
            this.Commands.AddTypeReader<Character>(new CharacterTypeReader());
            this.Commands.AddTypeReader<Roleplay>(new RoleplayTypeReader());
            this.Commands.AddTypeReader<Colour>(new ColourTypeReader());

            this.Commands.AddEnumReader<UserClass>();
            this.Commands.AddEnumReader<KinkPreference>();
            this.Commands.AddEnumReader<Bodypart>();
            this.Commands.AddEnumReader<Pattern>();
            this.Commands.AddEnumReader<Permission>();
            this.Commands.AddEnumReader<Permissions.PermissionTarget>();

            await this.Commands.AddModulesAsync(Assembly.GetEntryAssembly(), this.Services);
            await this.Behaviours.AddBehavioursAsync(Assembly.GetEntryAssembly(), this.Services);

            await this.Client.StartAsync();
            await this.Behaviours.StartBehavioursAsync();
        }

        /// <summary>
        /// Logs the ambassador out of Discord.
        /// </summary>
        /// <returns>A task representing the login action.</returns>
        public async Task LogoutAsync()
        {
            await this.Client.LogoutAsync();
        }

        /// <summary>
        /// Stops the ambassador, releasing its Discord resources.
        /// </summary>
        /// <returns>A task representing the stop action.</returns>
        public async Task StopAsync()
        {
            await this.Behaviours.StopBehavioursAsync();

            await LogoutAsync();
            await this.Client.StopAsync();
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
