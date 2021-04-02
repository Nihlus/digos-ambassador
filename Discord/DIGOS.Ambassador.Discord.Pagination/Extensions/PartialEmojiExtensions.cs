//
//  PartialEmojiExtensions.cs
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

using System;
using Remora.Discord.API.Abstractions.Objects;

namespace DIGOS.Ambassador.Discord.Pagination.Extensions
{
    /// <summary>
    /// Defines extension methods for the <see cref="IPartialEmoji"/> interface.
    /// </summary>
    public static class PartialEmojiExtensions
    {
        /// <summary>
        /// Gets the canonical name of the emoji.
        /// </summary>
        /// <param name="emoji">The emoji.</param>
        /// <returns>The name.</returns>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the emoji has neither an ID nor a name set.
        /// </exception>
        public static string GetEmojiName(this IPartialEmoji emoji)
        {
            if (emoji.Name.HasValue && emoji.Name.Value is not null)
            {
                return emoji.Name.Value;
            }

            if (!emoji.ID.HasValue)
            {
                throw new InvalidOperationException();
            }

            return emoji.ID.Value.ToString()!;
        }
    }
}
