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
using System.Linq;
using DIGOS.Ambassador.Discord.Extensions;
using Discord;

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
        public static IEnumerable<EmbedBuilder> FromFields
        (
            IEnumerable<EmbedFieldBuilder> fields,
            uint maxFieldsPerPage = 5,
            string description = "",
            EmbedBuilder? pageBase = null
        )
        {
            var enumeratedFields = fields.ToList();

            // Build the pages
            var pages = new List<EmbedBuilder>();

            if (pageBase is null)
            {
                pageBase = new EmbedBuilder();
                pageBase.WithDescription(description);
                pageBase.WithColor(Color.DarkPurple);
            }

            var currentPage = pageBase.CopyEmbedBuilder();
            foreach (var field in enumeratedFields)
            {
                var fieldContentLength = field.Name.Length + (field.Value.ToString()?.Length ?? 0);

                if (currentPage.Fields.Count >= maxFieldsPerPage || (currentPage.Length + fieldContentLength >= 1300))
                {
                    pages.Add(currentPage);

                    currentPage = pageBase.CopyEmbedBuilder();
                }

                currentPage.AddField(field);

                if (field == enumeratedFields.Last() && !pages.Contains(currentPage))
                {
                    pages.Add(currentPage);
                }
            }

            return pages;
        }
    }
}
