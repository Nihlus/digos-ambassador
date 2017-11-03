using System.Threading.Tasks;
using Discord.Commands;

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Admin & owner-only commands. These directly affect the bot on a global scale.
	/// </summary>
	[Group("admin")]
	public class AdminCommands : ModuleBase<SocketCommandContext>
	{
		[Command("update-kinks")]
		[Summary("Updates the kink list with data from F-list.")]
		public async Task UpdateKinkDatabaseAsync()
		{
			// Get the latest JSON from
		}
	}
}