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

using System.Threading.Tasks;
using Discord.Commands;

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Miscellaneous commands - just for fun, testing, etc.
	/// </summary>
	public class MiscellaneousCommands : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Sasses the user in a DIGOS fashion.
		/// </summary>
		/// <returns>A task wrapping the command.</returns>
		[Command("sass")]
		[Summary("Sasses the user in a DIGOS fashion.")]
		public async Task SassAsync()
		{
			string sass = ContentManager.Instance.GetSass(this.Context.Channel.IsNsfw);

			await this.Context.Channel.SendMessageAsync(sass);
		}
	}
}
