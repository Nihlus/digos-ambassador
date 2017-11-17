//
//  ContentService.cs
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database.Dossiers;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Services.Entity;

using Discord.Commands;

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services.Content
{
	/// <summary>
	/// Management class for content that comes bundled with the bot. Responsible for loading and providing access to
	/// the content.
	/// </summary>
	public sealed class ContentService
	{
		/// <summary>
		/// Gets the Discord bot OAuth token.
		/// </summary>o
		public string BotToken { get; private set; }

		private List<string> Sass;
		private List<string> SassNSFW;

		private Uri BaseRemoteContentUri { get; }

		/// <summary>
		/// Gets the base content path.
		/// </summary>
		public string BaseContentPath { get; } = Path.GetFullPath(Path.Combine("Content"));

		/// <summary>
		/// Gets the base dossier path.
		/// </summary>
		public string BaseDossierPath { get; } = Path.GetFullPath(Path.Combine("Content", "Dossiers"));

		/// <summary>
		/// Gets the <see cref="Uri"/> pointing to the default avatar used by the bot for characters.
		/// </summary>
		public Uri DefaultAvatarUri { get; }

		/// <summary>
		/// Gets the <see cref="Uri"/> pointing to a portrait of Amby.
		/// </summary>
		public Uri AmbyPortraitUri { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ContentService"/> class.
		/// </summary>
		public ContentService()
		{
			this.BaseRemoteContentUri = new Uri("https://github.com/Nihlus/digos-ambassador/raw/master/digos-ambassador/Content/");
			this.DefaultAvatarUri = new Uri(this.BaseRemoteContentUri, "Avatars/Default/Discord_DIGOS.png");

			this.AmbyPortraitUri = new Uri(this.BaseRemoteContentUri, "Portraits/amby-irbynx-3.png");
		}

		/// <summary>
		/// Loads the default content.
		/// </summary>
		/// <returns>A task wrapping the content load operation.</returns>
		public async Task InitializeAsync()
		{
			await LoadSassAsync();
			await LoadBotTokenAsync();

			if (!Directory.Exists(this.BaseDossierPath))
			{
				Directory.CreateDirectory(this.BaseDossierPath);
			}
		}

		/// <summary>
		/// Loads the sass from disk.
		/// </summary>
		private async Task LoadSassAsync()
		{
			var sassPath = Path.Combine(this.BaseContentPath, "Sass", "sass.txt");
			var sassNSFWPath = Path.Combine(this.BaseContentPath, "Sass", "sass-nsfw.txt");

			if (!File.Exists(sassPath))
			{
				this.Sass = new List<string>();
			}

			if (!File.Exists(sassNSFWPath))
			{
				this.SassNSFW = new List<string>();
			}

			this.Sass = (await File.ReadAllLinesAsync(sassPath)).ToList();
			this.SassNSFW = (await File.ReadAllLinesAsync(sassNSFWPath)).ToList();
		}

		/// <summary>
		/// Loads the bot token from disk.
		/// </summary>
		/// <exception cref="FileNotFoundException">Thrown if the bot token file can't be found.</exception>
		/// <exception cref="InvalidDataException">Thrown if no token exists in the file.</exception>
		private async Task LoadBotTokenAsync()
		{
			var tokenPath = Path.Combine(this.BaseContentPath, "bot.token");

			if (!File.Exists(tokenPath))
			{
				throw new FileNotFoundException("The bot token file could not be found.", tokenPath);
			}

			var token = await File.ReadAllTextAsync(tokenPath);

			if (string.IsNullOrEmpty(token))
			{
				throw new InvalidDataException("Missing bot token.");
			}

			this.BotToken = token;
		}

		/// <summary>
		/// Gets a given dossier's data.
		/// </summary>
		/// <param name="dossier">The dossier to get the data for.</param>
		/// <returns>A <see cref="FileStream"/> containing the dossier data.</returns>
		public RetrieveEntityResult<FileStream> GetDossierStream([NotNull] Dossier dossier)
		{
			if (!File.Exists(dossier.Path) || dossier.Path.IsNullOrWhitespace())
			{
				return RetrieveEntityResult<FileStream>.FromError(CommandError.ObjectNotFound, "No file data set.");
			}

			if (Directory.GetParent(dossier.Path).FullName != this.BaseDossierPath)
			{
				return RetrieveEntityResult<FileStream>.FromError(CommandError.Unsuccessful, "The dossier path pointed to something that wasn't in the dossier folder.");
			}

			return RetrieveEntityResult<FileStream>.FromSuccess(File.OpenRead(dossier.Path));
		}

		/// <summary>
		/// Deletes the content data associated with a given dossier.
		/// </summary>
		/// <param name="dossier">The dossier.</param>
		/// <returns>A deletion result which may or may not have succeeded.</returns>
		public Task<DeleteEntityResult> DeleteDossierDataAsync([NotNull] Dossier dossier)
		{
			var dataPath = GetDossierDataPath(dossier);
			if (!File.Exists(dataPath))
			{
				return Task.FromResult(DeleteEntityResult.FromSuccess());
			}

			try
			{
				File.Delete(dataPath);
			}
			catch (Exception e)
			{
				return Task.FromResult(DeleteEntityResult.FromError(CommandError.Exception, e.Message));
			}

			return Task.FromResult(DeleteEntityResult.FromSuccess());
		}

		/// <summary>
		/// Gets the absolute path to where the data of the dossier is stored.
		/// </summary>
		/// <param name="dossier">The dossier.</param>
		/// <returns>The path.</returns>
		[NotNull]
		public string GetDossierDataPath([NotNull] Dossier dossier)
		{
			return Path.GetFullPath(Path.Combine(this.BaseContentPath, "Dossiers", $"{dossier.Title}.pdf"));
		}

		/// <summary>
		/// Gets a sassy comment.
		/// </summary>
		/// <param name="includeNSFW">Whether or not to include NSFW sass.</param>
		/// <returns>A sassy comment.</returns>
		public string GetSass(bool includeNSFW = false)
		{
			if (includeNSFW)
			{
				return this.Sass.Union(this.SassNSFW).ToList().PickRandom();
			}

			return this.Sass.PickRandom();
		}
	}
}
