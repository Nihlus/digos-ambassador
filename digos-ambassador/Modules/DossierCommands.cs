//
//  DossierCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Affero General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Affero General Public License for more details.
//
//  You should have received a copy of the GNU Affero General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Dossiers;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
	/// <summary>
	/// Commands for viewing, adding, and editing dossier entries.
	/// </summary>
	[Group("dossier")]
	[Summary("Commands for viewing, adding, and editing dossier entries.")]
	public class DossierCommands : ModuleBase<SocketCommandContext>
	{
		private readonly UserFeedbackService Feedback;
		private readonly ContentService Content;
		private readonly DossierService Dossiers;

		/// <summary>
		/// Initializes a new instance of the <see cref="DossierCommands"/> class.
		/// </summary>
		/// <param name="feedback">The feedback service.</param>
		/// <param name="content">The content service.</param>
		/// <param name="dossiers">The dossier service.</param>
		public DossierCommands(UserFeedbackService feedback, ContentService content, DossierService dossiers)
		{
			this.Feedback = feedback;
			this.Content = content;
			this.Dossiers = dossiers;
		}

		/// <summary>
		/// Lists the available dossiers.
		/// </summary>
		[UsedImplicitly]
		[Command("list", RunMode = RunMode.Async)]
		[Summary("Lists the available dossiers.")]
		public async Task ListDossiersAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				var eb = BuildDossierListEmbed(db.Dossiers);
				await this.Feedback.SendEmbedAsync(this.Context, eb);
			}
		}

		[NotNull]
		private EmbedBuilder BuildDossierListEmbed([NotNull][ItemNotNull]IEnumerable<Dossier> dossiers)
		{
			var eb = this.Feedback.CreateBaseEmbed();
			eb.WithTitle("Dossier Database");

			foreach (var dossier in dossiers)
			{
				eb.AddField(dossier.Title, dossier.Summary);
			}

			if (!eb.Fields.Any())
			{
				eb.WithDescription("No dossiers found.");
			}

			return eb;
		}

		/// <summary>
		/// Views the named dossier.
		/// </summary>
		/// <param name="title">The title of the dossier to view.</param>
		[UsedImplicitly]
		[Alias("view", "show")]
		[Command("view", RunMode = RunMode.Async)]
		[Summary("Views the named dossier.")]
		public async Task ViewDossierAsync([NotNull] string title)
		{
			using (var db = new GlobalInfoContext())
			{
				var getDossierResult = await this.Dossiers.GetDossierByTitleAsync(db, title);
				if (!getDossierResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getDossierResult.ErrorReason);
					return;
				}

				var dossier = getDossierResult.Entity;

				var eb = BuildDossierEmbed(dossier);
				await this.Feedback.SendEmbedAsync(this.Context, eb);

				var dossierDataResult = this.Content.GetDossierStream(dossier);
				if (!dossierDataResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, dossierDataResult.ErrorReason);
					return;
				}

				using (var dossierData = dossierDataResult.Entity)
				{
					await this.Context.Channel.SendFileAsync(dossierData, $"{dossier.Title}.pdf");
				}
			}
		}

		private EmbedBuilder BuildDossierEmbed(Dossier dossier)
		{
			var eb = this.Feedback.CreateBaseEmbed();
			eb.WithTitle(dossier.Title);
			eb.WithDescription(dossier.Summary);

			return eb;
		}

		/// <summary>
		/// Adds a new dossier with the given title and summary. A PDF with the full dossier can be attached.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="summary">The summary.</param>
		[UsedImplicitly]
		[Alias("add", "create")]
		[Command("add", RunMode = RunMode.Async)]
		[Summary("Adds a new dossier with the given title and summary. A PDF with the full dossier can be attached.")]
		[RequireOwner]
		public async Task AddDossierAsync(string title, string summary = "No summary set.")
		{
			using (var db = new GlobalInfoContext())
			{
				var dossierCreationResult = await this.Dossiers.CreateDossierAsync(db, title, summary);
				if (!dossierCreationResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, dossierCreationResult.ErrorReason);
					return;
				}

				var dossier = dossierCreationResult.Entity;

				var modifyResult = await this.Dossiers.SetDossierDataAsync(db, dossier, this.Context);
				if (!modifyResult.IsSuccess)
				{
					if (modifyResult.Error == CommandError.Exception)
					{
						await this.Feedback.SendErrorAsync(this.Context, modifyResult.ErrorReason);
						await this.Dossiers.DeleteDossierAsync(db, dossier);
						return;
					}

					await this.Feedback.SendWarningAsync(this.Context, modifyResult.ErrorReason);
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Dossier \"{dossier.Title}\" added.");
			}
		}

		/// <summary>
		/// Removes the dossier with the given title.
		/// </summary>
		/// <param name="title">The title.</param>
		[UsedImplicitly]
		[Alias("remove", "delete")]
		[Command("remove")]
		[Summary("Removes the dossier with the given title.")]
		[RequireOwner]
		public async Task RemoveDossierAsync(string title)
		{
			using (var db = new GlobalInfoContext())
			{
				var getDossierResult = await this.Dossiers.GetDossierByTitleAsync(db, title);
				if (!getDossierResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getDossierResult.ErrorReason);
					return;
				}

				var dossier = getDossierResult.Entity;
				var deleteDossierResult = await this.Dossiers.DeleteDossierAsync(db, dossier);
				if (!deleteDossierResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getDossierResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Dossier \"{dossier.Title}\" deleted.");
			}
		}

		/// <summary>
		/// Setters for dossier properties.
		/// </summary>
		[Group("set")]
		public class SetCommands : ModuleBase<SocketCommandContext>
		{
			private readonly UserFeedbackService Feedback;
			private readonly DossierService Dossiers;

			/// <summary>
			/// Initializes a new instance of the <see cref="SetCommands"/> class.
			/// </summary>
			/// <param name="feedback">The feedback service.</param>
			/// <param name="dossiers">The dossier service.</param>
			public SetCommands(UserFeedbackService feedback, DossierService dossiers)
			{
				this.Feedback = feedback;
				this.Dossiers = dossiers;
			}

			/// <summary>
			/// Sets the title of the given dossier.
			/// </summary>
			/// <param name="title">The title of the dossier to edit.</param>
			/// <param name="newTitle">The new title of the dossier.</param>
			[UsedImplicitly]
			[Command("title", RunMode = RunMode.Async)]
			[Summary("Sets the title of the given dossier.")]
			[RequireOwner]
			public async Task SetTitleAsync(string title, string newTitle)
			{
				using (var db = new GlobalInfoContext())
				{
					var getDossierResult = await this.Dossiers.GetDossierByTitleAsync(db, title);
					if (!getDossierResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, getDossierResult.ErrorReason);
						return;
					}

					var dossier = getDossierResult.Entity;

					var modifyResult = await this.Dossiers.SetDossierTitleAsync(db, dossier, newTitle);
					if (!modifyResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, modifyResult.ErrorReason);
						return;
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "New dossier title set.");
				}
			}

			/// <summary>
			/// Sets the summary of the given dossier.
			/// </summary>
			/// <param name="title">The title of the dossier to edit.</param>
			/// <param name="newSummary">The new summary of the dossier.</param>
			[UsedImplicitly]
			[Command("summary", RunMode = RunMode.Async)]
			[Summary("Sets the summary of the given dossier.")]
			[RequireOwner]
			public async Task SetSummaryAsync(string title, string newSummary)
			{
				using (var db = new GlobalInfoContext())
				{
					var getDossierResult = await this.Dossiers.GetDossierByTitleAsync(db, title);
					if (!getDossierResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, getDossierResult.ErrorReason);
						return;
					}

					var dossier = getDossierResult.Entity;

					var modifyResult = await this.Dossiers.SetDossierSummaryAsync(db, dossier, newSummary);
					if (!modifyResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, modifyResult.ErrorReason);
						return;
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "New dossier summary set.");
				}
			}

			/// <summary>
			/// Sets the data of the given dossier. Attach a PDF to the command.
			/// </summary>
			/// <param name="title">The title of the dossier to edit.</param>
			[UsedImplicitly]
			[Command("data", RunMode = RunMode.Async)]
			[Summary("Sets the data of the given dossier. Attach a PDF to the command.")]
			[RequireOwner]
			public async Task SetFileAsync(string title)
			{
				using (var db = new GlobalInfoContext())
				{
					var getDossierResult = await this.Dossiers.GetDossierByTitleAsync(db, title);
					if (!getDossierResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, getDossierResult.ErrorReason);
						return;
					}

					var dossier = getDossierResult.Entity;

					var modifyResult = await this.Dossiers.SetDossierDataAsync(db, dossier, this.Context);
					if (!modifyResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, modifyResult.ErrorReason);
						return;
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "Dossier data set.");
				}
			}
		}
	}
}
