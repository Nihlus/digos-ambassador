//
//  UserCommands.cs
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
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.Permissions.Preconditions;

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionTarget;

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// User-related commands.
	/// </summary>
	[UsedImplicitly]
	[Group("user")]
	public class UserCommands : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Shows known information about the invoking user.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
		[UsedImplicitly]
		[Command("info")]
		[Summary("Shows known information about the invoking user.")]
		public async Task ShowInfoAsync()
		{
			User user;
			using (var db = new GlobalInfoContext())
			{
				user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);
			}

			await ShowUserInfoAsync(this.Context.Message.Author, user);
		}

		/// <summary>
		/// Shows known information about the mentioned user.
		/// </summary>
		/// <param name="discordUser">The Discord user to show the info of.</param>
		/// <returns>A task wrapping the command.</returns>
		[UsedImplicitly]
		[Command("info")]
		[Summary("Shows known information about the mentioned user.")]
		public async Task ShowInfoAsync(IUser discordUser)
		{
			User user;
			using (var db = new GlobalInfoContext())
			{
				user = await db.GetOrRegisterUserAsync(discordUser);
			}

			await ShowUserInfoAsync(discordUser, user);
		}

		/// <summary>
		/// Shows a nicely formatted info block about a user.
		/// </summary>
		/// <param name="discordUser">The Discord user to show the info of.</param>
		/// <param name="user">The stored information about the user.</param>
		/// <returns>A task wrapping the command.</returns>
		private async Task ShowUserInfoAsync([NotNull] IUser discordUser, [NotNull] User user)
		{
			var eb = new EmbedBuilder();

			eb.WithAuthor(discordUser);
			eb.WithThumbnailUrl(discordUser.GetAvatarUrl());

			switch (user.Class)
			{
				case UserClass.Other:
				{
					eb.WithColor(1.0f, 1.0f, 1.0f); // White
					break;
				}
				case UserClass.DIGOSInfrastructure:
				{
					eb.WithColor(Color.Purple);
					break;
				}
				case UserClass.DIGOSDronie:
				{
					eb.WithColor(Color.DarkOrange);
					break;
				}
				case UserClass.DIGOSUnit:
				{
					eb.WithColor(Color.DarkPurple);
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}

			eb.AddField("Name", discordUser.Username);
			eb.AddField("Class", user.Class.Humanize().Transform(To.TitleCase));

			string timezoneValue;
			if (user.Timezone == null)
			{
				timezoneValue = "No timezone set.";
			}
			else
			{
				timezoneValue = "UTC";
				if (user.Timezone >= 0)
				{
					timezoneValue += "+";
				}

				timezoneValue += user.Timezone.Value;
			}
			eb.AddField("Timezone", timezoneValue);

			string bioValue;
			if (string.IsNullOrEmpty(user.Bio))
			{
				bioValue = "No bio set.";
			}
			else
			{
				bioValue = user.Bio;
			}

			eb.AddField("Bio", bioValue);

			await this.Context.Channel.SendMessageAsync(string.Empty, false, eb);
		}

		/// <summary>
		/// User info edit and set commands
		/// </summary>
		[UsedImplicitly]
		[Group("set")]
		public class SetCommands : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// Sets the invoking user's class.
			/// </summary>
			/// <param name="userClass">The user's new class.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command("class")]
			[Summary("Sets the invoking user's class.")]
			[RequirePermission(SetClass)]
			public async Task SetUserClassAsync(UserClass userClass)
			{
				using (var db = new GlobalInfoContext())
				{
					// Add the user to the user database if they're not already in it
					var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);

					user.Class = userClass;

					await db.SaveChangesAsync();
				}

				await this.Context.Channel.SendMessageAsync("Class updated.");
			}

			/// <summary>
			/// Sets the target user's class.
			/// </summary>
			/// <param name="discordUser">The Discord user to change the class of.</param>
			/// <param name="userClass">The user's new class.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command("class")]
			[Summary("Sets the target user's class.")]
			[RequirePermission(SetClass, Other)]
			public async Task SetUserClassAsync(IUser discordUser, UserClass userClass)
			{
				using (var db = new GlobalInfoContext())
				{
					// Add the user to the user database if they're not already in it
					var user = await db.GetOrRegisterUserAsync(discordUser);

					user.Class = userClass;

					await db.SaveChangesAsync();
				}

				await this.Context.Channel.SendMessageAsync($"Class of {discordUser.Mention} updated.");
			}

			/// <summary>
			/// Sets the invoking user's bio.
			/// </summary>
			/// <param name="bio">The user's new bio.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command("bio")]
			[Summary("Sets the invoking user's bio.")]
			[RequirePermission(EditUser)]
			public async Task SetUserBioAsync(string bio)
			{
				using (var db = new GlobalInfoContext())
				{
					// Add the user to the user database if they're not already in it
					var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);

					user.Bio = bio;

					await db.SaveChangesAsync();
				}

				await this.Context.Channel.SendMessageAsync("Bio updated.");
			}

			/// <summary>
			/// Sets the target user's bio.
			/// </summary>
			/// <param name="discordUser">The Discord user to change the bio of.</param>
			/// <param name="bio">The user's new bio.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command("bio")]
			[Summary("Sets the target user's bio.")]
			[RequirePermission(EditUser, Other)]
			public async Task SetUserBioAsync(IUser discordUser, string bio)
			{
				using (var db = new GlobalInfoContext())
				{
					// Add the user to the user database if they're not already in it
					var user = await db.GetOrRegisterUserAsync(discordUser);

					user.Bio = bio;

					await db.SaveChangesAsync();
				}

				await this.Context.Channel.SendMessageAsync($"Bio of {discordUser.Mention} updated.");
			}

			/// <summary>
			/// Sets the invoking user's timezone hour offset.
			/// </summary>
			/// <param name="timezone">The user's new timezone hour offset.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command("timezone")]
			[Summary("Sets the invoking user's timezone hour offset.")]
			[RequirePermission(EditUser)]
			public async Task SetUserTimezoneAsync(int timezone)
			{
				using (var db = new GlobalInfoContext())
				{
					// Add the user to the user database if they're not already in it
					var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);

					user.Timezone = timezone;

					await db.SaveChangesAsync();
				}

				await this.Context.Channel.SendMessageAsync("Timezone updated.");
			}

			/// <summary>
			/// Sets the target user's timezone hour offset.
			/// </summary>
			/// <param name="discordUser">The Discord user to change the timezone of.</param>
			/// <param name="timezone">The user's new timezone hour offset.</param>
			/// <returns>A task wrapping the command.</returns>
			[UsedImplicitly]
			[Command("timezone")]
			[Summary("Sets the target user's timezone hour offset.")]
			[RequirePermission(EditUser, Other)]
			public async Task SetUserTimezoneAsync(IUser discordUser, int timezone)
			{
				using (var db = new GlobalInfoContext())
				{
					// Add the user to the user database if they're not already in it
					var user = await db.GetOrRegisterUserAsync(discordUser);

					user.Timezone = timezone;

					await db.SaveChangesAsync();
				}

				await this.Context.Channel.SendMessageAsync($"Timezone of {discordUser.Mention} updated.");
			}
		}
	}
}
