//
//  RoleplayCommands.cs
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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;
using Humanizer;
using Microsoft.EntityFrameworkCore;
using static DIGOS.Ambassador.Permissions.Permission;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

// ReSharper disable UnusedMember.Global
// ReSharper disable ArgumentsStyleLiteral
namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Commands for interacting with channel roleplays.
	/// </summary>
	[Alias("roleplay", "rp")]
	[Group("roleplay")]
	public class RoleplayCommands : ModuleBase<SocketCommandContext>
	{
		private readonly CommandService Commands;

		private readonly UserFeedbackService Feedback;

		/// <summary>
		/// Initializes a new instance of the <see cref="RoleplayCommands"/> class.
		/// </summary>
		/// <param name="commands">The command service.</param>
		/// <param name="feedback">The user feedback service.</param>
		public RoleplayCommands(CommandService commands, UserFeedbackService feedback)
		{
			this.Commands = commands;
			this.Feedback = feedback;
		}

		/// <summary>
		/// Shows information about the currently active roleplay in the channel.
		/// </summary>
		[Priority(-1)]
		[Command]
		[Summary("Shows information about the currently active roleplay in the channel.")]
		public async Task Default() => await ShowCurrentRoleplayAsync();

		/// <summary>
		/// Shows information about the currently active roleplay in the channel.
		/// </summary>
		[Command("show")]
		[Summary("Shows information about the currently active roleplay in the channel.")]
		public async Task ShowCurrentRoleplayAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				if (!await db.HasActiveRoleplayAsync(this.Context.Channel))
				{
					await this.Feedback.SendWarningAsync(this.Context.Channel, "There is no roleplay that is currently active in this channel.");
					return;
				}

				var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
				await ShowRoleplayAsync(roleplay);
			}
		}

		/// <summary>
		/// Shows information about the named roleplay.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay.</param>
		[Command("show")]
		[Summary("Shows information about the named roleplay.")]
		public async Task ShowRoleplayAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await GetNamedRoleplayAsync(db, roleplayName);
				if (roleplay is null)
				{
					return;
				}

				await ShowRoleplayAsync(roleplay);
			}
		}

		/// <summary>
		/// Shows information about the named roleplay owned by the specified user.
		/// </summary>
		/// <param name="discordUser">The user that owns the roleplay.</param>
		/// <param name="roleplayName">The name of the roleplay.</param>
		public async Task ShowRoleplayAsync(IUser discordUser, string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(discordUser, roleplayName);
				if (roleplay is null)
				{
					return;
				}

				await ShowRoleplayAsync(roleplay);
			}
		}

		private async Task ShowRoleplayAsync(Roleplay roleplay)
		{
			var eb = CreateRoleplayInfoEmbed(roleplay);
			await this.Context.Channel.SendMessageAsync(string.Empty, false, eb);
		}

		private EmbedBuilder CreateRoleplayInfoEmbed(Roleplay roleplay)
		{
			var eb = new EmbedBuilder();

			eb.WithAuthor(this.Context.Client.GetUser(roleplay.Owner.DiscordID));
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle(roleplay.Name);
			eb.WithDescription(roleplay.Summary);

			var participantUsers = roleplay.Participants.Select(p => this.Context.Client.GetUser(p.DiscordID));
			var participantMentions = participantUsers.Select(u => u.Mention);

			var participantList = participantMentions.Humanize();
			participantList = string.IsNullOrEmpty(participantList) ? "None" : participantList;

			eb.AddField("Participants", $"{participantList}");
			return eb;
		}

		/// <summary>
		/// Lists the roleplays that the user owns.
		/// </summary>
		[Command("list-owned")]
		[Summary("Lists the roleplays that the user owns.")]
		[RequirePermission(CreateRoleplay)]
		public async Task ListOwnedRoleplaysAsync()
		{
			var eb = new EmbedBuilder();
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle("Your roleplays");

			using (var db = new GlobalInfoContext())
			{
				var roleplays = db.GetUserRoleplays(this.Context.Message.Author);

				foreach (var roleplay in roleplays)
				{
					eb.AddField(roleplay.Name, roleplay.Summary);
				}
			}

			await this.Context.Channel.SendMessageAsync(string.Empty, false, eb);
		}

		/// <summary>
		/// Creates a new roleplay with the specified name.
		/// </summary>
		/// <param name="roleplayName">The user-unique name of the roleplay.</param>
		/// <param name="roleplaySummary">A summary of the roleplay.</param>
		/// <param name="isNSFW">Whether or not the roleplay is NSFW.</param>
		[Command("create")]
		[Summary("Creates a new roleplay with the specified name.")]
		[RequirePermission(CreateRoleplay)]
		public async Task CreateRoleplayAsync(string roleplayName, string roleplaySummary = "No summary set.", bool isNSFW = false)
		{
			using (var db = new GlobalInfoContext())
			{
				if (!IsRoleplayNameValid(roleplayName))
				{
					await this.Feedback.SendErrorAsync
					(
						this.Context.Channel,
						"That isn't a valid name for a roleplay."
					);
					return;
				}

				if (!await db.IsRoleplayNameUniqueForUserAsync(this.Context.Message.Author, roleplayName))
				{
					await this.Feedback.SendErrorAsync
					(
						this.Context.Channel,
						"You're already using that name for one of your RPs. Please pick another one, or delete the old one."
					);
					return;
				}

				var owner = await db.GetOrRegisterUserAsync(this.Context.Message.Author);
				var roleplay = new Roleplay
				{
					IsActive = false,
					IsNSFW = isNSFW,
					IsPublic = true,
					ActiveChannelID = this.Context.Channel.Id,
					Owner = owner,
					Participants = new List<User> { owner },
					Name = roleplayName,
					Summary = roleplaySummary
				};

				await db.Roleplays.AddAsync(roleplay);
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context.Channel, "Roleplay created.");
			}
		}

		/// <summary>
		/// Builds a list of the command names and aliases in this module, and checks that the given roleplay name is
		/// not one of them.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay.</param>
		/// <returns>true if the name is valid; otherwise, false.</returns>
		private bool IsRoleplayNameValid(string roleplayName)
		{
			var commandModule = this.Commands.Modules.First(m => m.Name == "roleplay");
			var submodules = commandModule.Submodules;

			var commandNames = commandModule.Commands.SelectMany(c => c.Aliases);
			commandNames = commandNames.Union(commandModule.Commands.Select(c => c.Name));

			var submoduleCommandNames = submodules.SelectMany(s => s.Commands.SelectMany(c => c.Aliases));
			submoduleCommandNames = submoduleCommandNames.Union(submodules.SelectMany(s => s.Commands.Select(c => c.Name)));

			commandNames = commandNames.Union(submoduleCommandNames);

			return !commandNames.Contains(roleplayName);
		}

		/// <summary>
		/// Deletes the roleplay with the specified name.
		/// </summary>
		/// <param name="roleplayName">The user-unique name of the roleplay.</param>
		[Command("delete")]
		[Summary("Deletes the roleplay with the specified name.")]
		[RequirePermission(DeleteRoleplay)]
		public async Task DeleteRoleplayAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
				if (roleplay is null)
				{
					await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
					return;
				}

				db.Roleplays.Remove(roleplay);
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context.Channel, "Roleplay deleted.");
			}
		}

		/// <summary>
		/// Joins the current roleplay.
		/// </summary>
		[Command("join")]
		[Summary("Joins the current roleplay.")]
		[RequirePermission(JoinRoleplay)]
		[RequireActiveRoleplay]
		public async Task JoinRoleplayAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
				await JoinRoleplayAsync(db, roleplay);
			}
		}

		/// <summary>
		/// Joins the roleplay with the given name.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to join.</param>
		[Command("join")]
		[Summary("Joins the roleplay with the given name.")]
		[RequirePermission(JoinRoleplay)]
		public async Task JoinRoleplayAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await GetNamedRoleplayAsync(db, roleplayName);
				if (roleplay is null)
				{
					return;
				}

				await JoinRoleplayAsync(db, roleplay);
			}
		}

		private async Task<Roleplay> GetNamedRoleplayAsync(GlobalInfoContext db, string roleplayName)
		{
			if (await db.Roleplays.CountAsync(rp => rp.Name == roleplayName) > 1)
			{
				await this.Feedback.SendWarningAsync(this.Context.Channel, "There's more than one roleplay with that name. Please specify which user it belongs to.");

				return null;
			}

			var roleplay = db.Roleplays
				.Include(rp => rp.Owner)
				.Include(rp => rp.Participants)
				.Include(rp => rp.Messages)
				.FirstOrDefault(rp => rp.Name == roleplayName);

			if (roleplay is null)
			{
				await this.Feedback.SendErrorAsync(this.Context.Channel, "No roleplay with that name found.");
			}

			return roleplay;
		}

		/// <summary>
		/// Joins the roleplay owned by the given person with the given name.
		/// </summary>
		/// <param name="roleplayOwner">The owner of the roleplay to join.</param>
		/// <param name="roleplayName">The name of the roleplay to join.</param>
		[Command("join")]
		[Summary("Joins the roleplay owned by the given person with the given name.")]
		[RequirePermission(JoinRoleplay)]
		public async Task JoinRoleplayAsync(IUser roleplayOwner, string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(roleplayOwner, roleplayName);
				await JoinRoleplayAsync(db, roleplay);
			}
		}

		private async Task JoinRoleplayAsync(GlobalInfoContext db, Roleplay roleplay)
		{
			if (roleplay is null)
			{
				await this.Feedback.SendWarningAsync(this.Context.Channel, "There is no roleplay with that name.");
				return;
			}

			if (roleplay.Participants.Any(p => p.DiscordID == this.Context.Message.Author.Id))
			{
				await this.Feedback.SendWarningAsync(this.Context.Channel, "You're already in that roleplay.");
				return;
			}

			roleplay.Participants.Add(await db.GetOrRegisterUserAsync(this.Context.Message.Author));
			await db.SaveChangesAsync();

			var roleplayOwnerUser = this.Context.Guild.GetUser(roleplay.Owner.DiscordID);
			await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"Joined {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\"");
		}

		/// <summary>
		/// Leaves the current roleplay.
		/// </summary>
		[Command("leave")]
		[Summary("Leaves the current roleplay.")]
		[RequireActiveRoleplay]
		public async Task LeaveRoleplayAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
				await LeaveRoleplayAsync(db, roleplay);
			}
		}

		/// <summary>
		/// Leaves the roleplay with the given name.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to leave.</param>
		[Command("leave")]
		[Summary("Leaves the roleplay with the given name.")]
		public async Task LeaveRoleplayAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await GetNamedRoleplayAsync(db, roleplayName);
				if (roleplay is null)
				{
					return;
				}

				await LeaveRoleplayAsync(db, roleplay);
			}
		}

		/// <summary>
		/// Leaves the roleplay owned by the given person with the given name.
		/// </summary>
		/// <param name="roleplayOwner">The owner of the roleplay to leave.</param>
		/// <param name="roleplayName">The name of the roleplay to leave.</param>
		[Command("leave")]
		[Summary("Leaves the roleplay owned by the given person with the given name.")]
		public async Task LeaveRoleplayAsync(IUser roleplayOwner, string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(roleplayOwner, roleplayName);
				await LeaveRoleplayAsync(db, roleplay);
			}
		}

		private async Task LeaveRoleplayAsync(GlobalInfoContext db, Roleplay roleplay)
		{
			if (!roleplay.Participants.Any(p => p.DiscordID == this.Context.Message.Author.Id))
			{
				await this.Feedback.SendWarningAsync(this.Context.Channel, "You're not in that roleplay.");
				return;
			}

			if (roleplay.Owner.DiscordID == this.Context.Message.Author.Id)
			{
				await this.Feedback.SendErrorAsync(this.Context.Channel, "You can't leave a roleplay that you own.");
				return;
			}

			roleplay.Participants = roleplay.Participants.Where(p => p.DiscordID != this.Context.Message.Author.Id).ToList();
			await db.SaveChangesAsync();

			var roleplayOwnerUser = this.Context.Guild.GetUser(roleplay.Owner.DiscordID);
			await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"Left {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\"");
		}

		/// <summary>
		/// Kicks the given user from the current roleplay.
		/// </summary>
		/// <param name="discordUser">The user to kick.</param>
		[Command("kick")]
		[Summary("Kicks the given user from the current roleplay.")]
		[RequireActiveRoleplay(requireOwner: true)]
		public async Task KickRoleplayParticipantAsync(IUser discordUser)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
				await KickRoleplayParticipantAsync(db, discordUser, roleplay);
			}
		}

		/// <summary>
		/// Kicks the given user from the named roleplay.
		/// </summary>
		/// <param name="discordUser">The user to kick.</param>
		/// <param name="roleplayName">The roleplay to kick them from.</param>
		[Command("kick")]
		[Summary("Kicks the given user from the named roleplay.")]
		public async Task KickRoleplayParticipantAsync(IUser discordUser, string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
				await KickRoleplayParticipantAsync(db, discordUser, roleplay);
			}
		}

		private async Task KickRoleplayParticipantAsync(GlobalInfoContext db, IUser discordUser, Roleplay roleplay)
		{
			if (roleplay is null)
			{
				await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
				return;
			}

			roleplay.Participants = roleplay.Participants.Where(p => p.DiscordID != discordUser.Id).ToList();
			await db.SaveChangesAsync();

			var userDMChannel = await discordUser.GetOrCreateDMChannelAsync();
			await userDMChannel.SendMessageAsync
			(
				$"You've been removed from the \"{roleplay.Name}\" by {this.Context.Message.Author.Username}."
			);
		}

		/// <summary>
		/// Makes the roleplay with the given name current in the current channel.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to make current.</param>
		[Command("make-current")]
		[Summary("Makes the roleplay with the given name current in the current channel.")]
		public async Task MakeRoleplayCurrentAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
				if (roleplay is null)
				{
					await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
					return;
				}

				if (roleplay.IsNSFW && !this.Context.Channel.IsNsfw)
				{
					await this.Feedback.SendErrorAsync(this.Context.Channel, "This channel is not marked as NSFW, while your roleplay is... naughty!");
					return;
				}

				roleplay.ActiveChannelID = this.Context.Channel.Id;
				await db.SaveChangesAsync();
			}

			await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"The roleplay \"{roleplayName}\" is now current in #{this.Context.Channel.Name}.");
		}

		/// <summary>
		/// Starts the roleplay with the given name.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to start.</param>
		[Command("start")]
		[Summary("Starts the roleplay with the given name.")]
		public async Task StartRoleplayAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
				if (roleplay is null)
				{
					await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
					return;
				}

				if (roleplay.IsNSFW && !this.Context.Channel.IsNsfw)
				{
					await this.Feedback.SendErrorAsync(this.Context.Channel, "This channel is not marked as NSFW, while your roleplay is... naughty!");
					return;
				}

				if (await db.HasActiveRoleplayAsync(this.Context.Channel))
				{
					await this.Feedback.SendWarningAsync(this.Context.Channel, "There's already a roleplay active in this channel.");

					var currentRoleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
					var timeOfLastMessage = currentRoleplay.Messages.Last().Timestamp;
					var currentTime = DateTimeOffset.Now;
					if (timeOfLastMessage < currentTime.AddHours(-6))
					{
						await this.Feedback.SendConfirmationAsync(this.Context.Channel, "However, that roleplay has been inactive for over six hours.");
						currentRoleplay.IsActive = false;
					}
					else
					{
						return;
					}
				}

				roleplay.IsActive = true;
				await db.SaveChangesAsync();
			}

			await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"The roleplay \"{roleplayName}\" is now active in {this.Context.Channel.Name}.");
		}

		/// <summary>
		/// Stops the current roleplay.
		/// </summary>
		[Command("stop")]
		[Summary("Stops the current roleplay.")]
		[RequireActiveRoleplay(requireOwner: true)]
		public async Task StopRoleplayAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				if (!await db.HasActiveRoleplayAsync(this.Context.Channel))
				{
					return;
				}

				var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);

				roleplay.IsActive = false;
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"The roleplay \"{roleplay.Name}\" has been stopped.");
			}
		}

		/// <summary>
		/// Transfers ownership of the current roleplay to the specified user.
		/// </summary>
		/// <param name="newOwner">The new owner.</param>
		[Command("transfer-ownership")]
		[Summary("Transfers ownership of the current roleplay to the specified user.")]
		[RequireActiveRoleplay(requireOwner: true)]
		public async Task TransferRoleplayOwnershipAsync(IUser newOwner)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
				await TransferRoleplayOwnershipAsync(db, newOwner, roleplay);
			}
		}

		/// <summary>
		/// Transfers ownership of the named roleplay to the specified user.
		/// </summary>
		/// <param name="newOwner">The new owner.</param>
		/// <param name="roleplayName">The name of the roleplay to transfer.</param>
		[Command("transfer-ownership")]
		[Summary("Transfers ownership of the named roleplay to the specified user.")]
		public async Task TransferRoleplayOwnershipAsync(IUser newOwner, string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
				await TransferRoleplayOwnershipAsync(db, newOwner, roleplay);
			}
		}

		private async Task TransferRoleplayOwnershipAsync(GlobalInfoContext db, IUser newOwner, Roleplay roleplay)
		{
			if (roleplay is null)
			{
				await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
				return;
			}

			if (db.GetUserRoleplays(newOwner).Any(rp => rp.Name == roleplay.Name))
			{
				await this.Feedback.SendErrorAsync(this.Context.Channel, $"That user already owns a roleplay named {roleplay.Name}. Please rename it first.");
				return;
			}

			var newUser = await db.GetOrRegisterUserAsync(newOwner);
			roleplay.Owner = newUser;

			await db.SaveChangesAsync();

			await this.Feedback.SendConfirmationAsync(this.Context.Channel, "Ownership transferred.");
		}

		/// <summary>
		/// Replays the current roleplay to you.
		/// </summary>
		/// <param name="from">The time from which you want to replay,</param>
		/// <param name="to">The time until you want to replay.</param>
		[Command("replay")]
		[Summary("Replays the current roleplay to you.")]
		[RequirePermission(ReplayRoleplay)]
		public async Task ReplayRoleplayAsync(DateTimeOffset from = default, DateTimeOffset to = default)
		{
			using (var db = new GlobalInfoContext())
			{
				if (!await db.HasActiveRoleplayAsync(this.Context.Channel))
				{
					await this.Feedback.SendWarningAsync(this.Context.Channel, "There's no active roleplay in this channel.");
					return;
				}

				var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);

				await ReplayRoleplayAsync(roleplay, from, to);
			}
		}

		/// <summary>
		/// Replays the named roleplay to you.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay.</param>
		/// <param name="from">The time from which you want to replay,</param>
		/// <param name="to">The time until you want to replay.</param>
		[Command("replay")]
		[Summary("Replays the named roleplay to you.")]
		[RequirePermission(ReplayRoleplay)]
		public async Task ReplayRoleplayAsync(string roleplayName, DateTimeOffset from = default, DateTimeOffset to = default)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await GetNamedRoleplayAsync(db, roleplayName);
				if (roleplay is null)
				{
					return;
				}

				await ReplayRoleplayAsync(roleplay, from, to);
			}
		}

		/// <summary>
		/// Replays the named roleplay owned by the given user to you.
		/// </summary>
		/// <param name="roleplayOwner">The owner of the roleplay.</param>
		/// <param name="roleplayName">The name of the roleplay.</param>
		/// <param name="from">The time from which you want to replay,</param>
		/// <param name="to">The time until you want to replay.</param>
		[Command("replay")]
		[Summary("Replays the named roleplay owned by the given user to you.")]
		[RequirePermission(ReplayRoleplay)]
		public async Task ReplayRoleplayAsync(IUser roleplayOwner, string roleplayName, DateTimeOffset from = default, DateTimeOffset to = default)
		{
			using (var db = new GlobalInfoContext())
			{
				var roleplay = await db.GetUserRoleplayByNameAsync(roleplayOwner, roleplayName);
				await ReplayRoleplayAsync(roleplay, from, to);
			}
		}

		private async Task ReplayRoleplayAsync(Roleplay roleplay, DateTimeOffset from = default, DateTimeOffset to = default)
		{
			if (roleplay is null)
			{
				await this.Feedback.SendErrorAsync(this.Context.Channel, "No roleplay with that name found.");
				return;
			}

			if (from == default)
			{
				from = DateTimeOffset.MinValue;
			}

			if (to == default)
			{
				to = DateTimeOffset.Now;
			}

			var userDMChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();
			var eb = CreateRoleplayInfoEmbed(roleplay);
			await userDMChannel.SendMessageAsync(string.Empty, false, eb);

			var messages = roleplay.Messages.Where(m => m.Timestamp > from && m.Timestamp < to).ToList();

			if (messages.Count <= 0)
			{
				await userDMChannel.SendMessageAsync("No messages found in the specified timeframe.");
				return;
			}

			await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"Replaying \"{roleplay.Name}\". Please check your private messages.");

			foreach (var message in messages)
			{
				await userDMChannel.SendMessageAsync($"{message.AuthorNickname}: {message.Contents}");
			}
		}

		/// <summary>
		/// Setter commands for roleplay properties.
		/// </summary>
		[Group("set")]
		public class SetCommands : ModuleBase<SocketCommandContext>
		{
			private readonly UserFeedbackService Feedback;

			/// <summary>
			/// Initializes a new instance of the <see cref="SetCommands"/> class.
			/// </summary>
			/// <param name="feedback">The user feedback service.</param>
			public SetCommands(UserFeedbackService feedback)
			{
				this.Feedback = feedback;
			}

			/// <summary>
			/// Sets the name of the current roleplay.
			/// </summary>
			/// <param name="newRoleplayName">The roleplay's new name.</param>
			[Command("name")]
			[Summary("Sets the new name of the named roleplay.")]
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplayNameAsync(string newRoleplayName)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
					await SetRoleplayNameAsync(db, roleplay, newRoleplayName);
				}
			}

			/// <summary>
			/// Sets the name of the named roleplay.
			/// </summary>
			/// <param name="oldRoleplayName">The name of the old roleplay.</param>
			/// <param name="newRoleplayName">The roleplay's new name.</param>
			[Command("name")]
			[Summary("Sets the new name of the named roleplay.")]
			public async Task SetRoleplayNameAsync(string oldRoleplayName, string newRoleplayName)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, oldRoleplayName);
					if (roleplay is null)
					{
						await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
						return;
					}

					await SetRoleplayNameAsync(db, roleplay, newRoleplayName);
				}
			}

			private async Task SetRoleplayNameAsync(GlobalInfoContext db, Roleplay roleplay, string newRoleplayName)
			{
				if (string.IsNullOrWhiteSpace(newRoleplayName))
				{
					await this.Feedback.SendErrorAsync(this.Context.Channel, "You need to provide a new name.");
					return;
				}

				if (!await db.IsRoleplayNameUniqueForUserAsync(this.Context.Message.Author, newRoleplayName))
				{
					await this.Feedback.SendErrorAsync(this.Context.Channel, "You already have a roleplay with that name.");
				}

				roleplay.Name = newRoleplayName;
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context.Channel, "Roleplay name set.");
			}

			/// <summary>
			/// Sets the summary of the current roleplay.
			/// </summary>
			/// <param name="newRoleplaySummary">The roleplay's new summary.</param>
			[Command("summary")]
			[Summary("Sets the summary of the current roleplay.")]
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplaySummaryAsync(string newRoleplaySummary)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
					await SetRoleplaySummaryAsync(db, roleplay, newRoleplaySummary);
				}
			}

			/// <summary>
			/// Sets the summary of the named roleplay.
			/// </summary>
			/// <param name="roleplayName">The name of the roleplay</param>
			/// <param name="newRoleplaySummary">The roleplay's new summary.</param>
			[Command("summary")]
			[Summary("Sets the summary of the current roleplay.")]
			public async Task SetRoleplaySummaryAsync(string roleplayName, string newRoleplaySummary)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
					if (roleplay is null)
					{
						await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
						return;
					}

					await SetRoleplaySummaryAsync(db, roleplay, newRoleplaySummary);
				}
			}

			private async Task SetRoleplaySummaryAsync(GlobalInfoContext db, Roleplay roleplay, string newRoleplaySummary)
			{
				if (string.IsNullOrWhiteSpace(newRoleplaySummary))
				{
					await this.Feedback.SendErrorAsync(this.Context.Channel, "You need to provide a summary.");
					return;
				}

				roleplay.Summary = newRoleplaySummary;
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context.Channel, "Roleplay summary set.");
			}

			/// <summary>
			/// Sets a value indicating whether or not the current roleplay is NSFW. This restricts which channels it
			/// can be made active in.
			/// </summary>
			/// <param name="isNSFW">true if the roleplay is NSFW; otherwise, false.</param>
			[RequireNsfw]
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplayIsNSFW(bool isNSFW)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
					await SetRoleplayIsNSFW(db, roleplay, isNSFW);
				}
			}

			/// <summary>
			/// Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it
			/// can be made active in.
			/// </summary>
			/// <param name="roleplayName">The name of the roleplay.</param>
			/// <param name="isNSFW">true if the roleplay is NSFW; otherwise, false.</param>
			[RequireNsfw]
			public async Task SetRoleplayIsNSFW(string roleplayName, bool isNSFW)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
					if (roleplay is null)
					{
						await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
						return;
					}

					await SetRoleplayIsNSFW(db, roleplay, isNSFW);
				}
			}

			private async Task SetRoleplayIsNSFW(GlobalInfoContext db, Roleplay roleplay, bool isNSFW)
			{
				if (roleplay.Messages.Count > 0 && roleplay.IsNSFW && !isNSFW)
				{
					await this.Feedback.SendErrorAsync(this.Context.Channel, "You can't mark a NSFW roleplay with messages in it as non-NSFW.");
					return;
				}

				roleplay.IsNSFW = isNSFW;
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"Roleplay set to {(isNSFW ? "NSFW" : "SFW")}");
			}

			/// <summary>
			/// Sets a value indicating whether or not the current roleplay is public. This restricts which users can
			/// view replays of it.
			/// </summary>
			/// <param name="isPublic">true if the roleplay is public; otherwise, false.</param>
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplayIsPublic(bool isPublic)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetActiveRoleplayAsync(this.Context.Channel);
					await SetRoleplayIsPublic(db, roleplay, isPublic);
				}
			}

			/// <summary>
			/// Sets a value indicating whether or not the named roleplay is publíc. This restricts which users can
			/// view replays of it.
			/// </summary>
			/// <param name="roleplayName">The name of the roleplay.</param>
			/// <param name="isPublic">true if the roleplay is public; otherwise, false.</param>
			public async Task SetRoleplayIsPublic(string roleplayName, bool isPublic)
			{
				using (var db = new GlobalInfoContext())
				{
					var roleplay = await db.GetUserRoleplayByNameAsync(this.Context.Message.Author, roleplayName);
					if (roleplay is null)
					{
						await this.Feedback.SendWarningAsync(this.Context.Channel, "You don't own a roleplay with that name.");
						return;
					}

					await SetRoleplayIsPublic(db, roleplay, isPublic);
				}
			}

			private async Task SetRoleplayIsPublic(GlobalInfoContext db, Roleplay roleplay, bool isPublic)
			{
				roleplay.IsNSFW = isPublic;
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context.Channel, $"Roleplay set to {(isPublic ? "public" : "private")}");
			}
		}
	}
}
