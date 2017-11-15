//
//  MiscellaneousCommands.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services.Content;
using DIGOS.Ambassador.Services.Feedback;

using Discord;
using Discord.Commands;
using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Miscellaneous commands - just for fun, testing, etc.
	/// </summary>
	[UsedImplicitly]
	[Name("miscellaneous")]
	public class MiscellaneousCommands : ModuleBase<SocketCommandContext>
	{
		private readonly CommandService Commands;

		private readonly ContentService Content;

		private readonly IServiceProvider Services;

		private readonly UserFeedbackService Feedback;

		/// <summary>
		/// Initializes a new instance of the <see cref="MiscellaneousCommands"/> class.
		/// </summary>
		/// <param name="commands">The command service.</param>
		/// <param name="content">The content service.</param>
		/// <param name="feedback">The user feedback service.</param>
		/// <param name="services">The service provider.</param>
		public MiscellaneousCommands(CommandService commands, ContentService content, UserFeedbackService feedback, IServiceProvider services)
		{
			this.Commands = commands;
			this.Content = content;
			this.Feedback = feedback;
			this.Services = services;
		}

		/// <summary>
		/// Sasses the user in a DIGOS fashion.
		/// </summary>
		[UsedImplicitly]
		[Command("sass", RunMode = RunMode.Async)]
		[Summary("Sasses the user in a DIGOS fashion.")]
		public async Task SassAsync()
		{
			string sass = this.Content.GetSass(this.Context.Channel.IsNsfw);

			await this.Feedback.SendConfirmationAsync(this.Context, sass);
		}

		/// <summary>
		/// Lists available commands.
		/// </summary>
		/// <param name="searchText">The text to search the command handler for.</param>
		[UsedImplicitly]
		[Alias("help", "halp", "hlep", "commands")]
		[Command("help", RunMode = RunMode.Async)]
		[Summary("Lists available commands")]
		public async Task HelpAsync([CanBeNull] string searchText = null)
		{
			IReadOnlyList<CommandInfo> searchResults;
			if (searchText.IsNullOrEmpty())
			{
				searchResults = this.Commands.Modules.SelectMany(m => m.Commands).ToList();
			}
			else
			{
				searchResults = this.Commands.Commands.Where
				(
					c =>
					c.Aliases.Any
					(
						a =>
						a.Contains(searchText)
					)
					|| c.Parameters.Any
					(
						p => p.Name.Contains(searchText)
					)
				)
				.ToList();
			}

			var userChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();

			var modules = GetTopLevelModules(searchResults.Select(ci => ci.Module)).Distinct();

			foreach (var module in modules)
			{
				var eb = new EmbedBuilder();

				eb.WithColor(Color.Blue);
				eb.WithDescription($"Available commands in {module.Name}");

				var matchingCommandsInModule = module.Commands.Union
				(
					module.Submodules.SelectMany
					(
						sm => sm.Commands
					)
				)
				.Where(c => searchResults.Contains(c));

				foreach (var command in matchingCommandsInModule)
				{
					var hasPermission = await command.CheckPreconditionsAsync(this.Context, this.Services);
					if (hasPermission.IsSuccess)
					{
						eb.AddField(command.Aliases.First(), $"{command.Summary}\n{this.Feedback.BuildParameterList(command)}");
					}
				}

				if (eb.Fields.Count > 0)
				{
					await userChannel.SendMessageAsync(string.Empty, false, eb);
				}
			}

			await this.Feedback.SendConfirmationAsync(this.Context, "Please check your private messages.");
			await userChannel.CloseAsync();
		}

		private IEnumerable<ModuleInfo> GetTopLevelModules(IEnumerable<ModuleInfo> childModules)
		{
			foreach (var childModule in childModules)
			{
				if (childModule.IsSubmodule)
				{
					foreach (var parentModule in GetTopLevelModules(new List<ModuleInfo> { childModule.Parent }))
					{
						yield return parentModule;
					}
				}
				else
				{
					yield return childModule;
				}
			}
		}
	}
}
