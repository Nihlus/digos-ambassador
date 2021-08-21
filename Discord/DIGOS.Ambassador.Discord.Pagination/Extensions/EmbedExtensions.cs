//
//  EmbedExtensions.cs
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

using Remora.Discord.API.Abstractions.Objects;

namespace DIGOS.Ambassador.Discord.Pagination.Extensions
{
    /// <summary>
    /// Defines extension methods for the <see cref="IEmbed"/> interface.
    /// </summary>
    public static class EmbedExtensions
    {
        /// <summary>
        /// Calculates the sum length of all elements in an embed which count towards Discord's internal limit.
        /// </summary>
        /// <param name="embed">The embed.</param>
        /// <returns>The length.</returns>
        public static int CalculateEmbedLength(this IEmbed embed)
        {
            var length = 0;

            if (embed.Title.IsDefined(out var title))
            {
                length += title.Length;
            }

            if (embed.Description.IsDefined(out var description))
            {
                length += description.Length;
            }

            if (embed.Fields.IsDefined(out var fields))
            {
                foreach (var field in fields)
                {
                    length += field.Name.Length;
                    length += field.Value.Length;
                }
            }

            if (embed.Author.IsDefined(out var author))
            {
                if (author.Name.IsDefined(out var name))
                {
                    length += name.Length;
                }
            }

            if (embed.Footer.IsDefined(out var footer))
            {
                length += footer.Text.Length;
            }

            return length;
        }
    }
}
