//
//  AdminCommands.cs
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
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.FList.Kinks;

using Discord.Commands;

using Newtonsoft.Json;

using Kink = DIGOS.Ambassador.Database.UserInfo.Kink;

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Admin and owner-only commands. These directly affect the bot on a global scale.
	/// </summary>
	[Group("admin")]
	public class AdminCommands : ModuleBase<SocketCommandContext>
	{
		/// <summary>
		/// Updates the kink database with data from F-list.
		/// </summary>
		/// <returns>A task wrapping the update action.</returns>
		[Command("update-kinks")]
		[Summary("Updates the kink list with data from F-list.")]
		[RequireOwner]
		public async Task UpdateKinkDatabaseAsync()
		{
			int updatedKinkCount = 0;

			// Get the latest JSON from F-list
			string json;
			using (var web = new HttpClient())
			{
				web.Timeout = TimeSpan.FromSeconds(3);

				var cts = new CancellationTokenSource();
				cts.CancelAfter(web.Timeout);

				try
				{
					using (var response = await web.GetAsync(new Uri("https://www.f-list.net/json/api/kink-list.php"), cts.Token))
					{
						json = await response.Content.ReadAsStringAsync();
					}
				}
				catch (OperationCanceledException)
				{
					await this.Context.Channel.SendMessageAsync("Could not connect to F-list: Operation timed out.");
					return;
				}
			}

			var kinkCollection = JsonConvert.DeserializeObject<KinkCollection>(json);
			using (var db = new GlobalInfoContext())
			{
				foreach (var kinkSection in kinkCollection.KinkCategories)
				{
					if (!Enum.TryParse<KinkCategory>(kinkSection.Key, out var kinkCategory))
					{
						await this.Context.Channel.SendMessageAsync("Failed to parse kink category.");
						return;
					}

					updatedKinkCount += await db.UpdateKinksAsync(kinkSection.Value.Kinks.Select
					(
						k => new Kink
						{
							Category = kinkCategory,
							Name = k.Name,
							Description = k.Description,
							FListID = k.KinkId
						}
					));
				}
			}

			await this.Context.Channel.SendMessageAsync($"Done. {updatedKinkCount} kinks updated.");
		}
	}
}
