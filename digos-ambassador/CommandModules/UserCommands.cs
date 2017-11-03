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
		}
	}
}
