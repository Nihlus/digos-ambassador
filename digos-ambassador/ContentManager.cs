//
//  ContentManager.cs
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

using System.IO;

namespace DIGOS.Ambassador
{
	/// <summary>
	/// Management class for content that comes bundled with the bot. Responsible for loading and providing access to
	/// the content.
	/// </summary>
	public static class ContentManager
	{
		/// <summary>
		/// Gets the OAuth token for logging into the bot.
		/// </summary>
		/// <returns>The OAuth token.</returns>
		public static string GetBotToken()
		{
			var tokenPath = Path.Combine("Content", "bot.token");

			if (!File.Exists(tokenPath))
			{
				throw new FileNotFoundException("The bot token file could not be found.", tokenPath);
			}

			var token = File.ReadAllText(tokenPath);

			if (string.IsNullOrEmpty(token))
			{
				throw new InvalidDataException("Missing bot token.");
			}

			return token;
		}
	}
}
