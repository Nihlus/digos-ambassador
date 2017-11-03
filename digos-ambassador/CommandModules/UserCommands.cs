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
using Discord;
using Discord.Commands;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.UserInfo;
using Humanizer;

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// User-related commands.
	/// </summary>
	[Group("user")]
	public class UserCommands : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Shows known information about the invoking user.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
		[Command("info")]
		[Summary("Shows known information about the invoking user.")]
		public async Task ShowInfoAsync()
		{
			User user;
			using (var db = new GlobalUserInfoContext())
			{
				user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);
			}

			await ShowUserInfoAsync(this.Context.Message.Author, user);
		}

		/// <summary>
		/// Shows known information about the mentioned user.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
		[Command("info")]
		[Summary("Shows known information about the mentioned user.")]
		public async Task ShowInfoAsync(IUser discordUser)
		{
			User user;
			using (var db = new GlobalUserInfoContext())
			{
				user = await db.GetOrRegisterUserAsync(discordUser);
			}

			await ShowUserInfoAsync(discordUser, user);
		}

		/// <summary>
		/// Shows a nicely formatted info block about a user.
		/// </summary>
		/// <param name="discordUser">The Discord user.</param>
		/// <param name="user">The stored information about the user.</param>
		/// <returns>A task wrapping the command.</returns>
		private async Task ShowUserInfoAsync(IUser discordUser, User user)
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

		[Group("set")]
		public class SetCommands : ModuleBase<SocketCommandContext>
		{
			/// <summary>
			/// Sets the user's class.
			/// </summary>
			/// <returns>A task wrapping the command.</returns>
			[Command("class")]
			[Summary("Shows known information about the invoking user.")]
			public async Task SetUserClass(UserClass userClass)
			{
				using (var db = new GlobalUserInfoContext())
				{
					// Add the user to the user database if they're not already in it
					var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);

					user.Class = userClass;

					await db.SaveChangesAsync();
				}

				await this.Context.Channel.SendMessageAsync("Class updated.");
			}
		}
	}
}
