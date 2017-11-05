//
//  AmbassadorClient.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
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

		private readonly CommandService Commands;

		private readonly IServiceProvider Services;

		/// <summary>
		/// Initializes a new instance of the <see cref="AmbassadorClient"/> class.
		/// </summary>
		public AmbassadorClient()
		{
			this.Client = new DiscordSocketClient();
			this.Client.Log += OnDiscordLogEvent;

			this.Commands = new CommandService();

			this.Services = new ServiceCollection()
				.AddSingleton(this.Client)
				.AddSingleton(this.Commands)
				.BuildServiceProvider();

			this.Client.MessageReceived += OnMessageReceived;
		}

		/// <summary>
		/// Logs the ambassador into Discord.
		/// </summary>
		/// <returns>A task representing the login action.</returns>
		public async Task LoginAsync()
		{
			await this.Client.LoginAsync(TokenType.Bot, ContentManager.Instance.BotToken);
		}

		/// <summary>
		/// Starts the ambassador, allowing it to react to messages.
		/// </summary>
		/// <returns>A task representing the start action.</returns>
		public async Task StartAsync()
		{
			await this.Commands.AddModulesAsync(Assembly.GetEntryAssembly());
			await this.Client.StartAsync();
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

			int argumentPos = 0;
			if (!(message.HasCharPrefix('!', ref argumentPos) || message.HasMentionPrefix(this.Client.CurrentUser, ref argumentPos)))
			{
				return;
			}

			var context = new SocketCommandContext(this.Client, message);
			var result = await this.Commands.ExecuteAsync(context, argumentPos, this.Services);

			if (!result.IsSuccess)
			{
				switch (result.Error)
				{
					case CommandError.UnknownCommand:
					{
						await context.Channel.SendMessageAsync("Unknown command.");
						break;
					}
					case CommandError.UnmetPrecondition:
					{
						await context.Channel.SendMessageAsync("You are not authorized to run that command.");
						break;
					}
					case CommandError.ParseFailed:
					case CommandError.BadArgCount:
					case CommandError.ObjectNotFound:
					case CommandError.MultipleMatches:
					case CommandError.Exception:
					case CommandError.Unsuccessful:
					{
						await context.Channel.SendMessageAsync($"Bzzt. Looks like we've had a wardrobe malfunction: {result.ErrorReason}");
						break;
					}
					case null:
					{
						await context.Channel.SendMessageAsync("Unknown error. Please contact maintenance.");
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
