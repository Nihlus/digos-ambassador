using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.UserInfo;
using DIGOS.Ambassador.FList.Kinks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Kink = DIGOS.Ambassador.Database.UserInfo.Kink;

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
		[RequireOwner]
		public async Task UpdateKinkDatabaseAsync()
		{
			int updatedKinks;
			// Get the latest JSON from F-list

			string json;
			using (var web = new HttpClient())
			{
				web.Timeout = TimeSpan.FromSeconds(3);

				var cts = new CancellationTokenSource();
				cts.CancelAfter(3000);

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

			var kinks = new List<Kink>();
			var kinkCollection = JsonConvert.DeserializeObject<KinkCollection>(json);
			foreach (var kinkSection in kinkCollection.KinkCategories)
			{
				if (!Enum.TryParse<KinkCategory>(kinkSection.Key, out var kinkCategory))
				{
					await this.Context.Channel.SendMessageAsync("Failed to parse kink category.");
					return;
				}

				kinks.AddRange(kinkSection.Value.Kinks.Select
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

			using (var db = new GlobalInfoContext())
			{
				updatedKinks = await db.UpdateKinksAsync(kinks);
			}

			await this.Context.Channel.SendMessageAsync($"Done. {updatedKinks} kinks updated.");
		}
	}
}
