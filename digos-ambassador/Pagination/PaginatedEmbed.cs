//
//  PaginatedEmbed.cs
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
using DIGOS.Ambassador.Services;
using Discord;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Pagination
{
    /// <summary>
    /// Represents a paginated gallery of embeds.
    /// </summary>
    public class PaginatedEmbed : PaginatedMessage<EmbedBuilder, PaginatedEmbed>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedEmbed"/> class.
        /// </summary>
        /// <param name="feedbackService">The feedback service.</param>
        public PaginatedEmbed(UserFeedbackService feedbackService)
            : base(feedbackService)
        {
        }

        /// <inheritdoc />
        public override Embed BuildEmbed(int page)
        {
            var currentPage = this.Pages[page];

            if (!(this.Appearance.Author is null))
            {
                currentPage = currentPage.WithAuthor(this.Appearance.Author);
            }

            return currentPage
            .WithFooter(f => f.Text = string.Format(this.Appearance.FooterFormat, page + 1, this.Pages.Count))
            .Build();
        }
    }
}
