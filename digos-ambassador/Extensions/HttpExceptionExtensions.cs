//
//  HttpExceptionExtensions.cs
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

using System.Net;
using Discord.Net;

namespace DIGOS.Ambassador.Extensions
{
	/// <summary>
	/// Extension methods for the <see cref="HttpException"/> class.
	/// </summary>
	public static class HttpExceptionExtensions
	{
		/// <summary>
		/// Determines whether or not the given <see cref="HttpException"/> was caused by the receiving user not
		/// accepting DMs from non-friends (e.g, we're not allowed to send messages to the user.
		/// </summary>
		/// <param name="this">The exception.</param>
		/// <returns>true if it was caused by the user not accepting DMs; Otherwise, false.</returns>
		public static bool WasCausedByDMsNotAccepted(this HttpException @this)
		{
			return @this.HttpCode == HttpStatusCode.Forbidden && @this.DiscordCode == 50007;
		}
	}
}
