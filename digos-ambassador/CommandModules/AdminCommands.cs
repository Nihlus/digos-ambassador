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
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.FList.Kinks;
using DIGOS.Ambassador.Services.Feedback;

using Discord.Commands;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

using Kink = DIGOS.Ambassador.Database.UserInfo.Kink;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Admin and owner-only commands. These directly affect the bot on a global scale.
	/// </summary>
	[UsedImplicitly]
	[Group("admin")]
	public class AdminCommands : ModuleBase<SocketCommandContext>
	{
		private readonly UserFeedbackService Feedback;

		/// <summary>
		/// Initializes a new instance of the <see cref="AdminCommands"/> class.
		/// </summary>
		/// <param name="feedback">The user feedback service.</param>
		public AdminCommands(UserFeedbackService feedback)
		{
			this.Feedback = feedback;
		}

		/// <summary>
		/// Updates the kink database with data from F-list.
		/// </summary>
		/// <returns>A task wrapping the update action.</returns>
		[UsedImplicitly]
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
					await this.Feedback.SendErrorAsync(this.Context, "Could not connect to F-list: Operation timed out.");
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
						await this.Feedback.SendErrorAsync(this.Context, "Failed to parse kink category.");
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

			await this.Feedback.SendConfirmationAsync(this.Context, $"Done. {updatedKinkCount} kinks updated.");
		}

		/// <summary>
		/// Wipes the database, resetting it to its initial state.
		/// </summary>
		[UsedImplicitly]
		[Alias("wipe-db", "reset-db")]
		[Command("wipe-db")]
		[Summary("Wipes the database, resetting it to its initial state.")]
		[RequireOwner]
		public async Task ResetDatabaseAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				await db.Database.EnsureDeletedAsync();
				await db.Database.MigrateAsync();

				await this.Feedback.SendConfirmationAsync(this.Context, "Database reset.");
			}
		}
	}
}
