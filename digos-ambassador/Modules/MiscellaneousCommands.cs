//
//  MiscellaneousCommands.cs
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

using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
	/// <summary>
	/// Assorted commands that don't really fit anywhere - just for fun, testing, etc.
	/// </summary>
	[UsedImplicitly]
	[Name("miscellaneous")]
	[Summary("Assorted commands that don't really fit anywhere - just for fun, testing, etc.")]
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
		/// Instructs Amby to contact you over DM.
		/// </summary>
		[UsedImplicitly]
		[Command("contact", RunMode = RunMode.Async)]
		[Summary("Instructs Amby to contact you over DM.")]
		[RequireContext(Guild)]
		public async Task ContactSelfAsync() => await ContactUserAsync(this.Context.User);

		/// <summary>
		/// Instructs Amby to contact a user over DM.
		/// </summary>
		/// <param name="discordUser">The user to contact.</param>
		[UsedImplicitly]
		[Command("contact", RunMode = RunMode.Async)]
		[Summary("Instructs Amby to contact a user over DM.")]
		[RequireContext(Guild)]
		[RequireUserPermission(GuildPermission.MentionEveryone)]
		public async Task ContactUserAsync([NotNull] IUser discordUser)
		{
			if (discordUser.Id == this.Context.Client.CurrentUser.Id)
			{
				await this.Feedback.SendErrorAsync(this.Context, "That's a splendid idea - at least then, I'd get an intelligent reply.");
				return;
			}

			if (discordUser.IsBot)
			{
				await this.Feedback.SendErrorAsync(this.Context, "I could do that, but I doubt I'd get a reply.");
				return;
			}

			var userDMChannel = await discordUser.GetOrCreateDMChannelAsync();

			var eb = this.Feedback.CreateFeedbackEmbed
			(
				discordUser,
				Color.DarkPurple,
				$"Hello there, {discordUser.Mention}. I've been instructed to initiate... negotiations... with you. \nA good place to start would be the \"!help <topic>\" command."
			);

			await userDMChannel.SendMessageAsync(string.Empty, false, eb);
			await userDMChannel.CloseAsync();
			await this.Feedback.SendConfirmationAsync(this.Context, "User contacted.");
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
		/// Boops the invoking user.
		/// </summary>
		[UsedImplicitly]
		[Command("boop", RunMode = RunMode.Async)]
		[Summary("Boops you.")]
		public async Task BoopAsync()
		{
			await this.Feedback.SendConfirmationAsync(this.Context, "*boop*");
		}

		/// <summary>
		/// Baps the invoking user.
		/// </summary>
		[UsedImplicitly]
		[Command("bap", RunMode = RunMode.Async)]
		[Summary("Baps you.")]
		public async Task BapAsync()
		{
			await this.Feedback.SendConfirmationAsync(this.Context, "**baps**");
		}

		/// <summary>
		/// Boops the target user.
		/// </summary>
		/// <param name="target">The target.</param>
		[UsedImplicitly]
		[Command("boop", RunMode = RunMode.Async)]
		[Summary("Boops the user.")]
		public async Task BoopAsync([NotNull] IUser target)
		{
			await this.Feedback.SendConfirmationAsync(this.Context, $"*boops {target.Mention}*");
		}

		/// <summary>
		/// Baps the target user.
		/// </summary>
		/// <param name="target">The target.</param>
		[UsedImplicitly]
		[Command("bap", RunMode = RunMode.Async)]
		[Summary("Baps the user.")]
		public async Task BapAsync([NotNull] IUser target)
		{
			await this.Feedback.SendConfirmationAsync(this.Context, $"**baps {target.Mention}**");
		}

		/// <summary>
		/// Shows some information about Amby's metaworkings.
		/// </summary>
		[UsedImplicitly]
		[Alias("info", "information", "about")]
		[Command("info", RunMode = RunMode.Async)]
		[Summary("Shows some information about Amby's metaworkings.")]
		public async Task InfoAsync()
		{
			var eb = this.Feedback.CreateBaseEmbed();

			eb.WithAuthor(this.Context.Client.CurrentUser);
			eb.WithTitle("The DIGOS Ambassador (\"Amby\")");
			eb.WithImageUrl(this.Content.AmbyPortraitUri.ToString());

			eb.WithDescription
			(
				"Amby is a Discord bot written in C# using the Discord.Net and EF Core frameworks. As an ambassador for " +
				"the DIGOS community, she provides a number of useful services for communities with similar interests - " +
				"namely, roleplaying, transformation, weird and wonderful sexual kinks, and much more.\n" +
				"\n" +
				"Amby is free and open source software, licensed under the AGPLv3. All of her source code can be freely " +
				"viewed and improved on Github at https://github.com/Nihlus/digos-ambassador. You are free to " +
				"run your own instance of Amby, redistribute her code, and modify it to your heart's content. If you're " +
				"not familiar with the AGPL, an excellent summary is available here: " +
				"https://choosealicense.com/licenses/agpl-3.0/.\n" +
				"\n" +
				"Any bugs you encounter should be reported on Github, following the issue template provided there. The " +
				"same holds for feature requests, for which a separate template is provided. Contributions in the form " +
				"of code, artwork, bug triaging, or quality control testing is always greatly appreciated!\n" +
				"\n" +
				"Stay sharky~\n" +
				"- Amby"
			);

			await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb);
		}

		/// <summary>
		/// Lists available commands modules.
		/// </summary>
		[UsedImplicitly]
		[Alias("help", "halp", "hlep", "commands")]
		[Command("help", RunMode = RunMode.Async)]
		[Summary("Lists available command modules.")]
		public async Task HelpAsync()
		{
			var eb = this.Feedback.CreateBaseEmbed();

			eb.WithTitle("Available command modules");
			eb.WithDescription
			(
				"To view commands in a specific module, use \"!help <topic>\", where the topic is a search string.\n" +
				"\n" +
				"Each command (in bold) can take zero or more parameters. These are listed after the short description " +
				"that follows each command. Parameters in brackets are optional."
			);

			foreach (var module in this.Commands.Modules.Where(m => !m.IsSubmodule))
			{
				eb.AddField(module.Name, module.Summary);
			}

			await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb);
		}

		/// <summary>
		/// Lists available commands based on a search string.
		/// </summary>
		/// <param name="searchText">The text to search the command handler for.</param>
		[UsedImplicitly]
		[Alias("help", "halp", "hlep", "commands")]
		[Command("help", RunMode = RunMode.Async)]
		[Summary("Lists available commands that match the given search text.")]
		public async Task HelpAsync([Remainder] [CanBeNull] string searchText)
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
						a.Contains(searchText, StringComparison.OrdinalIgnoreCase)
					)
					|| c.Module.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase)
				)
				.Distinct().ToList();
			}

			var userChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();
			if (searchResults.Count <= 0)
			{
				await this.Feedback.SendWarningAsync(this.Context, "No matching commands found.");
				return;
			}

			var modules = searchResults.Select(ci => ci.Module).GetTopLevelModules().Distinct();

			foreach (var module in modules)
			{
				var availableEmbed = new EmbedBuilder();

				var relevantModuleAliases = module.Aliases.Skip(1);
				var moduleExtraAliases = module.Aliases.Count > 1
					? $"(you can also use {relevantModuleAliases.Humanize("or")} instead of {module.Name})"
					: string.Empty;

				availableEmbed.WithColor(Color.Blue);
				availableEmbed.WithDescription($"Available commands in {module.Name} {moduleExtraAliases}");

				var unavailableEmbed = new EmbedBuilder();

				unavailableEmbed.WithColor(Color.Red);
				unavailableEmbed.WithDescription($"Unavailable commands in {module.Name} {moduleExtraAliases}");

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
					var relevantAliases = command.Aliases.Skip(1).Where(a => a.StartsWith(command.Module.Aliases.First())).ToList();
					var prefix = relevantAliases.Count > 1
						? "as well as"
						: "or";

					var commandExtraAliases = relevantAliases.Any()
						? $"({prefix} {relevantAliases.Humanize("or")})"
						: string.Empty;

					var commandDisplayAliases = $"{command.Aliases.First()} {commandExtraAliases}";

					var hasPermission = await command.CheckPreconditionsAsync(this.Context, this.Services);
					if (hasPermission.IsSuccess)
					{
						availableEmbed.AddField(commandDisplayAliases, $"{command.Summary}\n{this.Feedback.BuildParameterList(command)}");
					}
					else
					{
						unavailableEmbed.AddField(commandDisplayAliases, $"*{hasPermission.ErrorReason}*\n\n{command.Summary} \n{this.Feedback.BuildParameterList(command)}");
					}
				}

				if (availableEmbed.Fields.Count > 0)
				{
					await userChannel.SendMessageAsync(string.Empty, false, availableEmbed);
				}

				if (unavailableEmbed.Fields.Count > 0)
				{
					await userChannel.SendMessageAsync(string.Empty, false, unavailableEmbed);
				}
			}

			if (!this.Context.IsPrivate)
			{
				await this.Feedback.SendConfirmationAsync(this.Context, "Please check your private messages.");
			}

			await userChannel.CloseAsync();
		}
	}
}
