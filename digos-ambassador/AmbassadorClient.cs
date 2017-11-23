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
using System.Reflection;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Database.Users;
using DIGOS.Ambassador.Permissions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.TypeReaders;

using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;

using JetBrains.Annotations;
using log4net;
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

		private readonly IServiceProvider Services;

		/// <summary>
		/// Initializes a new instance of the <see cref="AmbassadorClient"/> class.
		/// </summary>
		/// <param name="content">The content service.</param>
		public AmbassadorClient([NotNull] ContentService content)
		{
			this.Client = new DiscordSocketClient();
			this.Client.Log += OnDiscordLogEvent;

			this.Commands = new CommandService();
			this.Commands.Log += OnDiscordLogEvent;

			this.DiscordIntegration = new DiscordService();
			this.Content = content;
			this.Commands = new CommandService();
			this.OwnedEntities = new OwnedEntityService();
			this.Roleplays = new RoleplayService(this.Commands, this.OwnedEntities);
			this.Characters = new CharacterService(this.Commands, this.OwnedEntities, this.Content);
			this.Feedback = new UserFeedbackService();
			this.Dossiers = new DossierService(this.Content);
			this.Interactive = new InteractiveService(this.Client);
			this.Transformation = new TransformationService(this.Content);

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
				.BuildServiceProvider();

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
			this.Commands.AddTypeReader<IMessage>(new UncachedMessageTypeReader<IMessage>());
			this.Commands.AddTypeReader<Character>(new CharacterTypeReader());
			this.Commands.AddTypeReader<Roleplay>(new RoleplayTypeReader());

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
				this.Roleplays.ConsumeMessage(arg);
				return;
			}

			// Perform first-time user checks, making sure the user has their default permissions
			using (var db = new GlobalInfoContext())
			{
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
					if (!server.KnownUsers.Any(u => u.UserID == user.UserID))
					{
						DefaultPermissions.Grant(server, user);
						server.KnownUsers.Add(user);

						await db.SaveChangesAsync();
					}
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
						await this.Feedback.SendWarningAsync(context, "Unknown command.");
						break;
					}
					case CommandError.ObjectNotFound:
					case CommandError.MultipleMatches:
					case CommandError.Unsuccessful:
					case CommandError.UnmetPrecondition:
					{
						await this.Feedback.SendErrorAsync(context, result.ErrorReason);
						break;
					}
					case CommandError.ParseFailed:
					case CommandError.BadArgCount:
					{
						await this.Feedback.SendErrorAsync(context, $"Command failed: {result.ErrorReason}");
						var searchResult = this.Commands.Search(context, argumentPos);

						var userDMChannel = await context.Message.Author.GetOrCreateDMChannelAsync();
						await userDMChannel.SendMessageAsync(string.Empty, false, this.Feedback.CreateCommandUsageEmbed(searchResult.Commands));
						break;
					}
					case CommandError.Exception:
					{
						await this.Feedback.SendErrorAsync(context, $"Bzzt: {result.ErrorReason}");
						break;
					}
					case null:
					{
						await this.Feedback.SendErrorAsync(context, "Unknown error. Please contact maintenance.");
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
		private async Task OnMessageUpdated(Cacheable<IMessage, ulong> oldMessage, SocketMessage updatedMessage, ISocketMessageChannel messageChannel)
		{
			if (updatedMessage is null)
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
