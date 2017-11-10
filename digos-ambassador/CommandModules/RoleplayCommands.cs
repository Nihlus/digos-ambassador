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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Roleplaying;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Roleplaying;
using DIGOS.Ambassador.TypeReaders;

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using static DIGOS.Ambassador.Permissions.Permission;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

// ReSharper disable ArgumentsStyleLiteral
namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Commands for interacting with channel roleplays.
	/// </summary>
	[UsedImplicitly]
	[Alias("roleplay", "rp")]
	[Group("roleplay")]
	public class RoleplayCommands : ModuleBase<SocketCommandContext>
	{
		private readonly RoleplayService Roleplays;

		private readonly UserFeedbackService Feedback;

		/// <summary>
		/// Initializes a new instance of the <see cref="RoleplayCommands"/> class.
		/// </summary>
		/// <param name="roleplays">The roleplay service.</param>
		/// <param name="feedback">The user feedback service.</param>
		public RoleplayCommands(RoleplayService roleplays, UserFeedbackService feedback)
		{
			this.Roleplays = roleplays;
			this.Feedback = feedback;
		}

		/// <summary>
		/// Shows information about the named roleplay owned by the specified user.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay.</param>
		/// <param name="discordUser">The user that owns the roleplay.</param>
		[UsedImplicitly]
		[Alias("show", "info")]
		[Command("show")]
		[Summary("Shows information about the specified roleplay.")]
		public async Task ShowRoleplayAsync(string roleplayName = null, IUser discordUser = null)
		{
			using (var db = new GlobalInfoContext())
			{
				var getRoleplayResult = await this.Roleplays.GetBestMatchingRoleplayAsync(db, this.Context, discordUser, roleplayName);
				if (!getRoleplayResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getRoleplayResult.ErrorReason);
					return;
				}

				var roleplay = getRoleplayResult.Entity;

				var eb = CreateRoleplayInfoEmbed(roleplay);
				await this.Feedback.SendEmbedAsync(this.Context, eb);
			}
		}

		[NotNull]
		private EmbedBuilder CreateRoleplayInfoEmbed([NotNull] Roleplay roleplay)
		{
			var eb = new EmbedBuilder();

			eb.WithAuthor(this.Context.Client.GetUser(roleplay.Owner.DiscordID));
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle(roleplay.Name);
			eb.WithDescription(roleplay.Summary);

			eb.AddInlineField("Currently", $"{(roleplay.IsActive ? "Active" : "Inactive")}");
			eb.AddInlineField("Channel", MentionUtils.MentionChannel(this.Context.Channel.Id));

			eb.AddField("NSFW", roleplay.IsNSFW ? "Yes" : "No");
			eb.AddInlineField("Public", roleplay.IsPublic ? "Yes" : "No");

			var participantUsers = roleplay.Participants.Select(p => this.Context.Client.GetUser(p.DiscordID));
			var participantMentions = participantUsers.Select(u => u.Mention);

			var participantList = participantMentions.Humanize();
			participantList = string.IsNullOrEmpty(participantList) ? "None" : participantList;

			eb.AddField("Participants", $"{participantList}");
			return eb;
		}

		/// <summary>
		/// Lists the roleplays that the given user owns.
		/// </summary>
		/// <param name="discordUser">The user to show the roleplays of.</param>
		[UsedImplicitly]
		[Command("list-owned")]
		[Summary("Lists the roleplays that the given user owns.")]
		public async Task ListOwnedRoleplaysAsync(IUser discordUser = null)
		{
			discordUser = discordUser ?? this.Context.Message.Author;

			var eb = new EmbedBuilder();
			eb.WithAuthor(discordUser);
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle("Your roleplays");

			using (var db = new GlobalInfoContext())
			{
				var roleplays = this.Roleplays.GetUserRoleplays(db, discordUser);

				foreach (var roleplay in roleplays)
				{
					eb.AddField(roleplay.Name, roleplay.Summary);
				}
			}

			await this.Feedback.SendEmbedAsync(this.Context, eb);
		}

		/// <summary>
		/// Creates a new roleplay with the specified name.
		/// </summary>
		/// <param name="roleplayName">The user-unique name of the roleplay.</param>
		/// <param name="roleplaySummary">A summary of the roleplay.</param>
		/// <param name="isNSFW">Whether or not the roleplay is NSFW.</param>
		/// <param name="isPublic">Whether or not the roleplay is public.</param>
		[UsedImplicitly]
		[Command("create")]
		[Summary("Creates a new roleplay with the specified name.")]
		[RequirePermission(CreateRoleplay)]
		public async Task CreateRoleplayAsync(string roleplayName, string roleplaySummary = "No summary set.", bool isNSFW = false, bool isPublic = true)
		{
			using (var db = new GlobalInfoContext())
			{
				var result = await this.Roleplays.CreateRoleplayAsync(db, this.Context, roleplayName, roleplaySummary, isNSFW, isPublic);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Roleplay \"{result.Entity.Name}\" created.");
			}
		}

		/// <summary>
		/// Deletes the roleplay with the specified name.
		/// </summary>
		/// <param name="roleplayName">The user-unique name of the roleplay.</param>
		[UsedImplicitly]
		[Command("delete")]
		[Summary("Deletes the roleplay with the specified name.")]
		[RequirePermission(DeleteRoleplay)]
		public async Task DeleteRoleplayAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var getRoleplayResult = await this.Roleplays.GetUserRoleplayByNameAsync(db, this.Context, this.Context.Message.Author, roleplayName);
				if (!getRoleplayResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getRoleplayResult.ErrorReason);
					return;
				}

				var roleplay = getRoleplayResult.Entity;

				db.Roleplays.Remove(roleplay);
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context, $"Roleplay \"{roleplay.Name}\" deleted.");
			}
		}

		/// <summary>
		/// Joins the roleplay owned by the given person with the given name.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to join.</param>
		/// <param name="roleplayOwner">The owner of the roleplay to join.</param>
		[UsedImplicitly]
		[Command("join")]
		[Summary("Joins the roleplay owned by the given person with the given name.")]
		[RequirePermission(JoinRoleplay)]
		public async Task JoinRoleplayAsync(string roleplayName = null, IUser roleplayOwner = null)
		{
			using (var db = new GlobalInfoContext())
			{
				var getRoleplayResult = await this.Roleplays.GetBestMatchingRoleplayAsync(db, this.Context, roleplayOwner, roleplayName);
				if (!getRoleplayResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getRoleplayResult.ErrorReason);
					return;
				}

				var roleplay = getRoleplayResult.Entity;
				var addUserResult = await this.Roleplays.AddUserToRoleplayAsync(db, this.Context, roleplay, this.Context.Message.Author);
				if (!addUserResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, addUserResult.ErrorReason);
					return;
				}

				var roleplayOwnerUser = this.Context.Guild.GetUser(roleplay.Owner.DiscordID);
				await this.Feedback.SendConfirmationAsync(this.Context, $"Joined {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\"");
			}
		}

		/// <summary>
		/// Leaves the roleplay owned by the given person with the given name.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to leave.</param>
		/// <param name="roleplayOwner">The owner of the roleplay to leave.</param>
		[UsedImplicitly]
		[Command("leave")]
		[Summary("Leaves the roleplay owned by the given person with the given name.")]
		public async Task LeaveRoleplayAsync(string roleplayName = null, IUser roleplayOwner = null)
		{
			using (var db = new GlobalInfoContext())
			{
				var getRoleplayResult = await this.Roleplays.GetBestMatchingRoleplayAsync(db, this.Context, roleplayOwner, roleplayName);
				if (!getRoleplayResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getRoleplayResult.ErrorReason);
					return;
				}

				var roleplay = getRoleplayResult.Entity;
				var removeUserResult = await this.Roleplays.RemoveUserFromRoleplayAsync(db, this.Context, roleplay, this.Context.Message.Author);
				if (!removeUserResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, removeUserResult.ErrorReason);
					return;
				}

				var roleplayOwnerUser = this.Context.Guild.GetUser(roleplay.Owner.DiscordID);
				await this.Feedback.SendConfirmationAsync(this.Context, $"Left {roleplayOwnerUser.Mention}'s roleplay \"{roleplay.Name}\"");
			}
		}

		/// <summary>
		/// Kicks the given user from the named roleplay.
		/// </summary>
		/// <param name="discordUser">The user to kick.</param>
		/// <param name="roleplayName">The roleplay to kick them from.</param>
		[UsedImplicitly]
		[Command("kick")]
		[Summary("Kicks the given user from the named roleplay.")]
		public async Task KickRoleplayParticipantAsync(IUser discordUser, string roleplayName = null)
		{
			using (var db = new GlobalInfoContext())
			{
				var getRoleplayResult = await this.Roleplays.GetBestMatchingRoleplayAsync(db, this.Context, this.Context.Message.Author, roleplayName);
				if (!getRoleplayResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getRoleplayResult.ErrorReason);
					return;
				}

				var roleplay = getRoleplayResult.Entity;

				var kickUserResult = await this.Roleplays.RemoveUserFromRoleplayAsync(db, this.Context, roleplay, discordUser);
				if (!kickUserResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, kickUserResult.ErrorReason);
					return;
				}

				var userDMChannel = await discordUser.GetOrCreateDMChannelAsync();
				await userDMChannel.SendMessageAsync
				(
					$"You've been removed from the \"{roleplay.Name}\" by {this.Context.Message.Author.Username}."
				);
			}
		}

		/// <summary>
		/// Makes the roleplay with the given name current in the current channel.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to make current.</param>
		[UsedImplicitly]
		[Command("make-current")]
		[Summary("Makes the roleplay with the given name current in the current channel.")]
		public async Task MakeRoleplayCurrentAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var result = await this.Roleplays.GetUserRoleplayByNameAsync(db, this.Context, this.Context.Message.Author, roleplayName);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				var roleplay = result.Entity;
				if (roleplay.IsNSFW && !this.Context.Channel.IsNsfw)
				{
					await this.Feedback.SendErrorAsync(this.Context, "This channel is not marked as NSFW, while your roleplay is... naughty!");
					return;
				}

				roleplay.ActiveChannelID = this.Context.Channel.Id;
				await db.SaveChangesAsync();
			}

			await this.Feedback.SendConfirmationAsync(this.Context, $"The roleplay \"{roleplayName}\" is now current in #{this.Context.Channel.Name}.");
		}

		/// <summary>
		/// Starts the roleplay with the given name.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay to start.</param>
		[UsedImplicitly]
		[Command("start")]
		[Summary("Starts the roleplay with the given name.")]
		public async Task StartRoleplayAsync(string roleplayName)
		{
			using (var db = new GlobalInfoContext())
			{
				var newRoleplayResult = await this.Roleplays.GetUserRoleplayByNameAsync(db, this.Context, this.Context.Message.Author, roleplayName);
				if (!newRoleplayResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, newRoleplayResult.ErrorReason);
					return;
				}

				var roleplay = newRoleplayResult.Entity;

				if (roleplay.IsNSFW && !this.Context.Channel.IsNsfw)
				{
					await this.Feedback.SendErrorAsync(this.Context, "This channel is not marked as NSFW, while your roleplay is... naughty!");
					return;
				}

				if (await this.Roleplays.HasActiveRoleplayAsync(db, this.Context.Channel))
				{
					await this.Feedback.SendWarningAsync(this.Context, "There's already a roleplay active in this channel.");

					var currentRoleplayResult = await this.Roleplays.GetActiveRoleplayAsync(db, this.Context.Channel);
					if (!currentRoleplayResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, currentRoleplayResult.ErrorReason);
						return;
					}

					var currentRoleplay = currentRoleplayResult.Entity;
					var timeOfLastMessage = currentRoleplay.Messages.Last().Timestamp;
					var currentTime = DateTimeOffset.Now;
					if (timeOfLastMessage < currentTime.AddHours(-4))
					{
						await this.Feedback.SendConfirmationAsync(this.Context, "However, that roleplay has been inactive for over four hours.");
						currentRoleplay.IsActive = false;
					}
					else
					{
						return;
					}
				}

				if (roleplay.ActiveChannelID != this.Context.Channel.Id)
				{
					roleplay.ActiveChannelID = this.Context.Channel.Id;
				}

				roleplay.IsActive = true;
				await db.SaveChangesAsync();

				var participantUsers = roleplay.Participants.Select(p => this.Context.Client.GetUser(p.DiscordID));
				var participantMentions = participantUsers.Select(u => u.Mention);

				var participantList = participantMentions.Humanize();
				await this.Feedback.SendConfirmationAsync(this.Context, $"The roleplay \"{roleplayName}\" is now active in {MentionUtils.MentionChannel(this.Context.Channel.Id)}.");
				await this.Context.Channel.SendMessageAsync($"Calling {participantList}!");
			}
		}

		/// <summary>
		/// Stops the current roleplay.
		/// </summary>
		[UsedImplicitly]
		[Command("stop")]
		[Summary("Stops the current roleplay.")]
		[RequireActiveRoleplay(requireOwner: true)]
		public async Task StopRoleplayAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				var result = await this.Roleplays.GetActiveRoleplayAsync(db, this.Context.Channel);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				var roleplay = result.Entity;
				roleplay.IsActive = false;
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context, $"The roleplay \"{roleplay.Name}\" has been stopped.");
			}
		}

		/// <summary>
		/// Includes previous messages into the roleplay, starting at the given time.
		/// </summary>
		/// <param name="startMessage">The earliest message to start adding from.</param>
		/// <param name="finalMessage">The final message in the range.</param>
		[UsedImplicitly]
		[Command("include-previous")]
		[Summary("Includes previous messages into the roleplay, starting at the given message.")]
		[RequireActiveRoleplay(requireOwner: true)]
		public async Task IncludePreviousMessagesAsync
		(
			[OverrideTypeReader(typeof(UncachedMessageTypeReader<IMessage>))] IMessage startMessage,
			[OverrideTypeReader(typeof(UncachedMessageTypeReader<IMessage>))] IMessage finalMessage = null
		)
		{
			finalMessage = finalMessage ?? this.Context.Message;

			using (var db = new GlobalInfoContext())
			{
				var result = await this.Roleplays.GetActiveRoleplayAsync(db, this.Context.Channel);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				int addedOrUpdatedMessageCount = 0;
				var roleplay = result.Entity;
				IMessage latestMessage = startMessage;
				while (latestMessage.Timestamp < finalMessage.Timestamp)
				{
					var messages = (await this.Context.Channel.GetMessagesAsync(latestMessage, Direction.After).Flatten()).OrderBy(m => m.Timestamp).ToList();
					latestMessage = messages.Last();

					foreach (var message in messages)
					{
						// Jump out if we've passed the final message
						if (message.Timestamp > finalMessage.Timestamp)
						{
							break;
						}

						var modifyResult = await this.Roleplays.AddToOrUpdateMessageInRoleplay(db, roleplay, message);
						if (modifyResult.IsSuccess)
						{
							++addedOrUpdatedMessageCount;
						}
					}
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"{addedOrUpdatedMessageCount} messages added to \"{roleplay.Name}\".");
			}
		}

		/// <summary>
		/// Transfers ownership of the named roleplay to the specified user.
		/// </summary>
		/// <param name="newOwner">The new owner.</param>
		/// <param name="roleplayName">The name of the roleplay to transfer.</param>
		[UsedImplicitly]
		[Command("transfer-ownership")]
		[Summary("Transfers ownership of the named roleplay to the specified user.")]
		public async Task TransferRoleplayOwnershipAsync(IUser newOwner, string roleplayName = null)
		{
			using (var db = new GlobalInfoContext())
			{
				var result = await this.Roleplays.GetBestMatchingRoleplayAsync(db, this.Context, this.Context.Message.Author, roleplayName);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				var roleplay = result.Entity;
				var transferResult = await this.Roleplays.TransferRoleplayOwnershipAsync(db, newOwner, roleplay);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, transferResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Ownership transferred.");
			}
		}

		/// <summary>
		/// Replays the named roleplay owned by the given user to you.
		/// </summary>
		/// <param name="roleplayName">The name of the roleplay.</param>
		/// <param name="roleplayOwner">The owner of the roleplay.</param>
		/// <param name="from">The time from which you want to replay,</param>
		/// <param name="to">The time until you want to replay.</param>
		[UsedImplicitly]
		[Command("replay")]
		[Summary("Replays the named roleplay owned by the given user to you.")]
		[RequirePermission(ReplayRoleplay)]
		public async Task ReplayRoleplayAsync(string roleplayName = null, IUser roleplayOwner = null, DateTimeOffset from = default, DateTimeOffset to = default)
		{
			using (var db = new GlobalInfoContext())
			{
				var result = await this.Roleplays.GetBestMatchingRoleplayAsync(db, this.Context, roleplayOwner, roleplayName);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				var roleplay = result.Entity;
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

				var messages = roleplay.Messages.Where(m => m.Timestamp > from && m.Timestamp < to).OrderBy(msg => msg.Timestamp).ToList();

				if (messages.Count <= 0)
				{
					await userDMChannel.SendMessageAsync("No messages found in the specified timeframe.");
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Replaying \"{roleplay.Name}\". Please check your private messages.");

				const int messageCharacterLimit = 2000;
				var sb = new StringBuilder(messageCharacterLimit);

				foreach (var message in messages)
				{
					if (sb.Length + message.Contents.Length > messageCharacterLimit)
					{
						await userDMChannel.SendMessageAsync(sb.ToString());
						sb.Clear();
						sb.AppendLine();
					}

					sb.AppendLine($"**{message.AuthorNickname}** {message.Contents}");

					if (message == messages.Last())
					{
						await userDMChannel.SendMessageAsync(sb.ToString());
					}
				}
			}
		}

		/// <summary>
		/// Setter commands for roleplay properties.
		/// </summary>
		[UsedImplicitly]
		[Group("set")]
		public class SetCommands : ModuleBase<SocketCommandContext>
		{
			private readonly RoleplayService Roleplays;

			private readonly UserFeedbackService Feedback;

			/// <summary>
			/// Initializes a new instance of the <see cref="SetCommands"/> class.
			/// </summary>
			/// <param name="roleplays">The roleplay service.</param>
			/// <param name="feedback">The user feedback service.</param>
			public SetCommands(RoleplayService roleplays, UserFeedbackService feedback)
			{
				this.Roleplays = roleplays;
				this.Feedback = feedback;
			}

			/// <summary>
			/// Sets the name of the current roleplay.
			/// </summary>
			/// <param name="newRoleplayName">The roleplay's new name.</param>
			[UsedImplicitly]
			[Command("name")]
			[Summary("Sets the new name of the named roleplay.")]
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplayNameAsync(string newRoleplayName)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetActiveRoleplayAsync(db, this.Context.Channel);
					await SetRoleplayNameAsync(db, result.Entity, newRoleplayName);
				}
			}

			/// <summary>
			/// Sets the name of the named roleplay.
			/// </summary>
			/// <param name="oldRoleplayName">The name of the old roleplay.</param>
			/// <param name="newRoleplayName">The roleplay's new name.</param>
			[UsedImplicitly]
			[Command("name")]
			[Summary("Sets the new name of the named roleplay.")]
			public async Task SetRoleplayNameAsync(string oldRoleplayName, string newRoleplayName)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetUserRoleplayByNameAsync(db, this.Context, this.Context.Message.Author, oldRoleplayName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var roleplay = result.Entity;

					await SetRoleplayNameAsync(db, roleplay, newRoleplayName);
				}
			}

			private async Task SetRoleplayNameAsync([NotNull] GlobalInfoContext db, [NotNull] Roleplay roleplay, [NotNull] string newRoleplayName)
			{
				var result = await this.Roleplays.SetRoleplayNameAsync(db, this.Context, roleplay, newRoleplayName);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Roleplay name set.");
			}

			/// <summary>
			/// Sets the summary of the current roleplay.
			/// </summary>
			/// <param name="newRoleplaySummary">The roleplay's new summary.</param>
			[UsedImplicitly]
			[Command("summary")]
			[Summary("Sets the summary of the current roleplay.")]
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplaySummaryAsync(string newRoleplaySummary)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetActiveRoleplayAsync(db, this.Context.Channel);
					await SetRoleplaySummaryAsync(db, result.Entity, newRoleplaySummary);
				}
			}

			/// <summary>
			/// Sets the summary of the named roleplay.
			/// </summary>
			/// <param name="roleplayName">The name of the roleplay</param>
			/// <param name="newRoleplaySummary">The roleplay's new summary.</param>
			[UsedImplicitly]
			[Command("summary")]
			[Summary("Sets the summary of the named roleplay.")]
			public async Task SetRoleplaySummaryAsync(string roleplayName, string newRoleplaySummary)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetUserRoleplayByNameAsync(db, this.Context, this.Context.Message.Author, roleplayName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var roleplay = result.Entity;

					await SetRoleplaySummaryAsync(db, roleplay, newRoleplaySummary);
				}
			}

			private async Task SetRoleplaySummaryAsync([NotNull] GlobalInfoContext db, [NotNull] Roleplay roleplay, [NotNull] string newRoleplaySummary)
			{
				var result = await this.Roleplays.SetRoleplaySummaryAsync(db, roleplay, newRoleplaySummary);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Roleplay summary set.");
			}

			/// <summary>
			/// Sets a value indicating whether or not the current roleplay is NSFW. This restricts which channels it
			/// can be made active in.
			/// </summary>
			/// <param name="isNSFW">true if the roleplay is NSFW; otherwise, false.</param>
			[UsedImplicitly]
			[Command("nsfw")]
			[Summary("Sets a value indicating whether or not the current roleplay is NSFW. This restricts which channels it can be made active in.")]
			[RequireNsfw]
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplayIsNSFW(bool isNSFW)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetActiveRoleplayAsync(db, this.Context.Channel);
					await SetRoleplayIsNSFW(db, result.Entity, isNSFW);
				}
			}

			/// <summary>
			/// Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it
			/// can be made active in.
			/// </summary>
			/// <param name="roleplayName">The name of the roleplay.</param>
			/// <param name="isNSFW">true if the roleplay is NSFW; otherwise, false.</param>
			[UsedImplicitly]
			[Command("nsfw")]
			[Summary("Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it can be made active in.")]
			[RequireNsfw]
			public async Task SetRoleplayIsNSFW(string roleplayName, bool isNSFW)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetUserRoleplayByNameAsync(db, this.Context, this.Context.Message.Author, roleplayName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var roleplay = result.Entity;

					await SetRoleplayIsNSFW(db, roleplay, isNSFW);
				}
			}

			private async Task SetRoleplayIsNSFW([NotNull] GlobalInfoContext db, [NotNull] Roleplay roleplay, bool isNSFW)
			{
				var result = await this.Roleplays.SetRoleplayIsNSFWAsync(db, roleplay, isNSFW);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Roleplay set to {(isNSFW ? "NSFW" : "SFW")}");
			}

			/// <summary>
			/// Sets a value indicating whether or not the current roleplay is public. This restricts replays to participants.
			/// </summary>
			/// <param name="isPublic">true if the roleplay is public; otherwise, false.</param>
			[UsedImplicitly]
			[Command("public")]
			[Summary("Sets a value indicating whether or not the current roleplay is public. This restricts replays to participants.")]
			[RequireActiveRoleplay(requireOwner: true)]
			public async Task SetRoleplayIsPublic(bool isPublic)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetActiveRoleplayAsync(db, this.Context.Channel);
					await SetRoleplayIsPublic(db, result.Entity, isPublic);
				}
			}

			/// <summary>
			/// Sets a value indicating whether or not the named roleplay is publíc. This restricts replays to participants.
			/// </summary>
			/// <param name="roleplayName">The name of the roleplay.</param>
			/// <param name="isPublic">true if the roleplay is public; otherwise, false.</param>
			[UsedImplicitly]
			[Command("public")]
			[Summary("Sets a value indicating whether or not the named roleplay is public. This restricts replays to participants.")]
			public async Task SetRoleplayIsPublic(string roleplayName, bool isPublic)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Roleplays.GetUserRoleplayByNameAsync(db, this.Context, this.Context.Message.Author, roleplayName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var roleplay = result.Entity;

					await SetRoleplayIsPublic(db, roleplay, isPublic);
				}
			}

			private async Task SetRoleplayIsPublic([NotNull] GlobalInfoContext db, [NotNull] Roleplay roleplay, bool isPublic)
			{
				var result = await this.Roleplays.SetRoleplayIsPublicAsync(db, roleplay, isPublic);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Roleplay set to {(isPublic ? "public" : "private")}");
			}
		}
	}
}
