//
//  DiscordUserExtensions.cs
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

using Discord;

namespace DIGOS.Ambassador.Discord.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="IUser"/> interface.
    /// </summary>
    public static class DiscordUserExtensions
    {
        /// <summary>
        /// Determines whether or not the given user is the same as the bot user.
        /// </summary>
        /// <param name="this">The user.</param>
        /// <param name="client">The context of the check.</param>
        /// <returns>true if the user is the same as the bot user; otherwise, false.</returns>
        public static bool IsMe(this IUser @this, IDiscordClient client)
        {
            return @this.Id == client.CurrentUser.Id;
        }
    }
}
