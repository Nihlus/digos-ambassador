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
using System.Reflection;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Kinks;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Transformations;
using DIGOS.Ambassador.TypeReaders;
using DIGOS.Ambassador.Utility;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using Humanizer;
using JetBrains.Annotations;
using log4net;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

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

		// Services should be persisted in the client
		// ReSharper disable PrivateFieldCanBeConvertedToLocalVariable
		private readonly DiscordSocketClient Client;

		private readonly DiscordService DiscordIntegration;

		private readonly OwnedEntityService OwnedEntities;

		private readonly ContentService Content;

		private readonly CommandService Commands;

		private readonly RoleplayService Roleplays;

		private readonly CharacterService Characters;

		private readonly UserFeedbackService Feedback;

		private readonly DossierService Dossiers;

		private readonly InteractiveService Interactive;

		private readonly TransformationService Transformation;

		private readonly LuaService Lua;

		private readonly KinkService Kinks;

		private readonly PermissionService Permissions;

		private readonly IServiceProvider Services;

		// ReSharper restore PrivateFieldCanBeConvertedToLocalVariable

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

			this.DiscordIntegration = new DiscordService();
			this.Content = content;
			this.Commands = new CommandService();
			this.OwnedEntities = new OwnedEntityService();
			this.Roleplays = new RoleplayService(this.Commands, this.OwnedEntities);
			this.Transformation = new TransformationService(this.Content);

			this.Characters = new CharacterService(this.Commands, this.OwnedEntities, this.Content, this.Transformation);
			this.Characters.DiscoverPronounProviders();

			this.Feedback = new UserFeedbackService();
			this.Dossiers = new DossierService(this.Content);
			this.Interactive = new InteractiveService(this.Client);

			this.Lua = new LuaService(this.Content);
			this.Kinks = new KinkService(this.Feedback);

			this.Permissions = new PermissionService();

			this.Services = new ServiceCollection()
				.AddSingleton(this.Client)
				.AddSingleton(this.DiscordIntegration)
				.AddSingleton(this.Content)
				.AddSingleton(this.Commands)
				.AddSingleton(this.Roleplays)
				.AddSingleton(this.Characters)
				.AddSingleton(this.Feedback)
				.AddSingleton(this.Dossiers)
				.AddSingleton(this.Interactive)
				.AddSingleton(this.Transformation)
				.AddSingleton(this.Lua)
				.AddSingleton(this.Kinks)
				.AddSingleton(this.Permissions)
				.AddDbContextPool<GlobalInfoContext>(builder => GlobalInfoContext.ConfigureOptions(builder))
				.BuildServiceProvider();

			this.Transformation = this.Transformation
			.WithDescriptionBuilder
			(
				ActivatorUtilities.CreateInstance<TransformationDescriptionBuilder>(this.Services)
			);

			this.Client.MessageReceived += OnMessageReceived;
			this.Client.MessageUpdated += OnMessageUpdated;
		}

		/// <summary>
		/// Logs the ambassador into Discord.
		/// </summary>
		/// <returns>A task representing the login action.</returns>
		public async Task LoginAsync()
		{
			await this.Client.LoginAsync(TokenType.Bot, this.Content.BotToken);
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
			this.Commands.AddTypeReader<UserClass>(new HumanizerEnumTypeReader<UserClass>());
			this.Commands.AddTypeReader<KinkPreference>(new HumanizerEnumTypeReader<KinkPreference>());
			this.Commands.AddTypeReader<Bodypart>(new HumanizerEnumTypeReader<Bodypart>());
			this.Commands.AddTypeReader<Pattern>(new HumanizerEnumTypeReader<Pattern>());

			await this.Commands.AddModulesAsync(Assembly.GetEntryAssembly());

			await this.Client.StartAsync();
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
			await this.Client.StopAsync();
		}

		/// <summary>
		/// Handles incoming messages, passing them to the command context handler.
		/// </summary>
		/// <param name="arg">The message coming in from the socket client.</param>
		/// <returns>A task representing the message handling.</returns>
		private async Task OnMessageReceived(SocketMessage arg)
		{
			var db = this.Services.GetRequiredService<GlobalInfoContext>();

			if (!(arg is SocketUserMessage message))
			{
				return;
			}

			if (arg.Author.IsBot)
			{
				return;
			}

			int argumentPos = 0;
			if (!(message.HasCharPrefix('!', ref argumentPos) || message.HasMentionPrefix(this.Client.CurrentUser, ref argumentPos)))
			{
				await this.Roleplays.ConsumeMessageAsync(db, new SocketCommandContext(this.Client, message));
				return;
			}

			// Perform first-time user checks, making sure the user has their default permissions
			var guild = (message.Channel as SocketGuildChannel)?.Guild;
			if (guild != null)
			{
				var user = await db.GetOrRegisterUserAsync(arg.Author);
				var server = await db.GetOrRegisterServerAsync(guild);

				if (server.KnownUsers is null)
				{
					server.KnownUsers = new List<User>();
				}

				// Grant permissions to new users
				if (!server.IsUserKnown(arg.Author))
				{
					await this.Permissions.GrantDefaultPermissionsAsync(db, guild, arg.Author);
					server.KnownUsers.Add(user);

					await db.SaveChangesAsync();
				}
			}

			var context = new SocketCommandContext(this.Client, message);

			var result = await this.Commands.ExecuteAsync(context, argumentPos, this.Services);

			if (!result.IsSuccess)
			{
				switch (result.Error)
				{
					case CommandError.UnknownCommand:
					{
						break;
					}
					case CommandError.ObjectNotFound:
					case CommandError.MultipleMatches:
					case CommandError.Unsuccessful:
					case CommandError.UnmetPrecondition:
					case CommandError.ParseFailed:
					case CommandError.BadArgCount:
					case CommandError.Exception:
					{
						var userDMChannel = await context.Message.Author.GetOrCreateDMChannelAsync();

						var errorEmbed = this.Feedback.CreateFeedbackEmbed(context.User, Color.Red, result.ErrorReason);
						var searchResult = this.Commands.Search(context, argumentPos);

						await userDMChannel.SendMessageAsync(string.Empty, false, errorEmbed);
						await userDMChannel.SendMessageAsync(string.Empty, false, this.Feedback.CreateCommandUsageEmbed(searchResult.Commands));
						break;
					}
					default:
					{
						throw new ArgumentOutOfRangeException();
					}
				}
			}
		}

		/// <summary>
		/// Handles reparsing of edited messages.
		/// </summary>
		/// <param name="oldMessage">The old message.</param>
		/// <param name="updatedMessage">The new message.</param>
		/// <param name="messageChannel">The channel of the message.</param>
		private async Task OnMessageUpdated(Cacheable<IMessage, ulong> oldMessage, [CanBeNull] SocketMessage updatedMessage, ISocketMessageChannel messageChannel)
		{
			if (updatedMessage is null)
			{
				return;
			}

			// Ignore all changes except text changes
			bool isTextUpdate = updatedMessage.EditedTimestamp.HasValue && (updatedMessage.EditedTimestamp.Value > DateTimeOffset.Now - 1.Minutes());
			if (!isTextUpdate)
			{
				return;
			}

			await OnMessageReceived(updatedMessage);
		}

		/// <summary>
		/// Saves log events from Discord using the configured method in log4net.
		/// </summary>
		/// <param name="arg">The log message from Discord</param>
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
