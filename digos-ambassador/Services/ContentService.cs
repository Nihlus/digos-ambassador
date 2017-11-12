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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Extensions;

namespace DIGOS.Ambassador.Services
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

		/// <summary>
		/// Loads the default content.
		/// </summary>
		/// <returns>A task wrapping the content load operation.</returns>
		public async Task LoadContentAsync()
		{
			await LoadSassAsync();
			await LoadBotTokenAsync();
		}

		/// <summary>
		/// Loads the sass from disk.
		/// </summary>
		private async Task LoadSassAsync()
		{
			var sassPath = Path.Combine("Content", "Sass", "sass.txt");
			var sassNSFWPath = Path.Combine("Content", "Sass", "sass-nsfw.txt");

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
			var tokenPath = Path.Combine("Content", "bot.token");

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
