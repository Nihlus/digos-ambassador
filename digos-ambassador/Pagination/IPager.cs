//
//  IPager.cs
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
using Discord;

namespace DIGOS.Ambassador.Pagination
{
    /// <summary>
    /// Interface for paginated content.
    /// </summary>
    /// <typeparam name="T1">The type of content in the pager.</typeparam>
    /// <typeparam name="T2">The type of the pager.</typeparam>
    public interface IPager<T1, out T2> where T2 : IPager<T1, T2>
    {
        /// <summary>
        /// Gets the pages in the pager.
        /// </summary>
        IList<T1> Pages { get; }

        /// <summary>
        /// Gets or sets the appearance options for the pager.
        /// </summary>
        PaginatedAppearanceOptions Appearance { get; set; }

        /// <summary>
        /// Appends a page to the pager.
        /// </summary>
        /// <param name="page">The page to add.</param>
        /// <returns>The pager with the page.</returns>
        T2 AppendPage(T1 page);

        /// <summary>
        /// Replaces the pages in the pager with a new collection.
        /// </summary>
        /// <param name="pages">The pages to add.</param>
        /// <returns>The pager with the pages.</returns>
        T2 WithPages(IEnumerable<T1> pages);

        /// <summary>
        /// Builds the embed for the pager content.
        /// </summary>
        /// <param name="page">The index of the page to display.</param>
        /// <returns>An embed to show to the user.</returns>
        Embed BuildEmbed(int page);
    }
}
