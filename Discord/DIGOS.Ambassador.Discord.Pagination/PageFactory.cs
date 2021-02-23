//
//  PageFactory.cs
//
//  Author:
//        Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.Drawing;
using System.Linq;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace DIGOS.Ambassador.Discord.Pagination
{
    /// <summary>
    /// Factory class for creating page collections from various sources.
    /// </summary>
    public static class PageFactory
    {
        /// <summary>
        /// Creates a set of embed pages from a collection of embed fields.
        /// </summary>
        /// <param name="fields">The fields to paginate.</param>
        /// <param name="maxFieldsPerPage">The maximum number of embed fields per page.</param>
        /// <param name="description">The description to display on each page.</param>
        /// <param name="pageBase">The base layout for the page.</param>
        /// <returns>The paginated embed.</returns>
        public static IEnumerable<Embed> FromFields
        (
            IEnumerable<IEmbedField> fields,
            uint maxFieldsPerPage = 5,
            string description = "",
            Embed? pageBase = null
        )
        {
            pageBase ??= new Embed
            {
                Description = description,
                Colour = Color.Purple
            };

            var pageBaseLength = CalculatePageBaseLength(pageBase);
            var enumeratedFields = fields.ToList();

            // Build the pages
            var pages = new List<Embed>();
            var currentPageFields = new List<IEmbedField>();
            if (pageBase.Fields.HasValue && pageBase.Fields.Value is not null)
            {
                currentPageFields.AddRange(pageBase.Fields.Value);
            }

            foreach (var field in enumeratedFields)
            {
                var fieldContentLength = field.Name.Length + field.Value.Length;
                if (currentPageFields.Count >= maxFieldsPerPage || (pageBaseLength + fieldContentLength >= 1300))
                {
                    pages.Add(pageBase with { Fields = new List<IEmbedField>(currentPageFields) });
                    currentPageFields.Clear();
                }

                currentPageFields.Add(field);
            }

            // Stick the remaining ones on the end
            if (currentPageFields.Count > 0)
            {
                pages.Add(pageBase with { Fields = new List<IEmbedField>(currentPageFields) });
                currentPageFields.Clear();
            }

            return pages;
        }

        private static int CalculatePageBaseLength(IEmbed pageBase)
        {
            var length = 0;

            if (pageBase.Title.HasValue)
            {
                length += pageBase.Title.Value.Length;
            }

            if (pageBase.Description.HasValue)
            {
                length += pageBase.Description.Value.Length;
            }

            if (pageBase.Fields.HasValue)
            {
                foreach (var field in pageBase.Fields.Value)
                {
                    length += field.Name.Length;
                    length += field.Value.Length;
                }
            }

            if (pageBase.Author.HasValue)
            {
                if (pageBase.Author.Value.Name.HasValue)
                {
                    length += pageBase.Author.Value.Name.Value.Length;
                }
            }

            if (pageBase.Footer.HasValue)
            {
                length += pageBase.Footer.Value.Text.Length;
            }

            return length;
        }
    }
}
