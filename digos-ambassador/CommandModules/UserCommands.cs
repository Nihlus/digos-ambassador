using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.UserInfo;

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
				// Add the user to the user database if they're not already in it
				user = db.GetOrRegisterUser(this.Context.Message.Author);
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
				user = db.GetOrRegisterUser(discordUser);
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

			eb.WithAuthor(this.Context.User);
			eb.WithThumbnailUrl(discordUser.GetAvatarUrl());

			switch (user.Class)
			{
				case UserClass.Other:
				{
					eb.WithColor(1.0f, 1.0f, 1.0f); // White
					break;
				}
				case UserClass.DIGOSDronie:
				{
					eb.WithColor(Color.DarkOrange);
					break;
				}
				case UserClass.DIGOSUnit:
				{
					eb.WithColor(Color.Purple);
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}

			eb.AddField("Name", discordUser.Username);
			eb.AddField("Class", user.Class);

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
	}
}
