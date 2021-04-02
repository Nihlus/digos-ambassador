//
//  PaginatedMessage.cs
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
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using DIGOS.Ambassador.Discord.Pagination.Extensions;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Discord.Pagination
{
    /// <summary>
    /// A page building class for paginated galleries.
    /// </summary>
    public class PaginatedMessage : InteractiveMessage
    {
        /// <summary>
        /// Gets the names of the reactions, mapped to their emoji.
        /// </summary>
        public IReadOnlyDictionary<string, IEmoji> ReactionNames { get; }

        /// <summary>
        /// Gets the pages in the message.
        /// </summary>
        public IReadOnlyList<Embed> Pages { get; }

        /// <summary>
        /// Gets the appearance options for the message.
        /// </summary>
        public PaginatedAppearanceOptions Appearance { get; }

        /// <summary>
        /// Gets the ID of the source user.
        /// </summary>
        public Snowflake SourceUserID { get; }

        /// <summary>
        /// Gets or sets the current page index.
        /// </summary>
        public int CurrentPage { get; set;  }

        /// <summary>
        /// Initializes a new instance of the <see cref="PaginatedMessage"/> class.
        /// </summary>
        /// <param name="channelID">The ID of the channel the message is in.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="sourceUserID">The ID of the source user.</param>
        /// <param name="pages">The pages in the paginated message.</param>
        /// <param name="appearance">The appearance options.</param>
        public PaginatedMessage
        (
            Snowflake channelID,
            Snowflake messageID,
            Snowflake sourceUserID,
            IReadOnlyList<Embed> pages,
            PaginatedAppearanceOptions? appearance = null
        )
            : base(channelID, messageID)
        {
            appearance ??= PaginatedAppearanceOptions.Default;

            this.SourceUserID = sourceUserID;
            this.Pages = pages;
            this.Appearance = appearance;

            this.ReactionNames = new Dictionary<string, IEmoji>
            {
                { appearance.First.GetEmojiName(), appearance.First },
                { appearance.Back.GetEmojiName(), appearance.Back },
                { appearance.Next.GetEmojiName(), appearance.Next },
                { appearance.Last.GetEmojiName(), appearance.Last },
                { appearance.Close.GetEmojiName(), appearance.Close },
                { appearance.Help.GetEmojiName(), appearance.Help }
            };
        }

        /// <summary>
        /// Moves the paginated message to the next page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MoveNext()
        {
            if (this.CurrentPage >= this.Pages.Count - 1)
            {
                return false;
            }

            this.CurrentPage += 1;
            return true;
        }

        /// <summary>
        /// Moves the paginated message to the previous page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MovePrevious()
        {
            if (this.CurrentPage <= 0)
            {
                return false;
            }

            this.CurrentPage -= 1;
            return true;
        }

        /// <summary>
        /// Moves the paginated message to the first page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MoveFirst()
        {
            if (this.CurrentPage == 0)
            {
                return false;
            }

            this.CurrentPage = 0;
            return true;
        }

        /// <summary>
        /// Moves the paginated message to the last page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MoveLast()
        {
            if (this.CurrentPage == this.Pages.Count - 1)
            {
                return false;
            }

            this.CurrentPage = this.Pages.Count - 1;
            return true;
        }

        /// <summary>
        /// Gets the current page.
        /// </summary>
        /// <returns>The page.</returns>
        public Embed GetCurrentPage()
        {
            return this.Pages[this.CurrentPage] with
            {
                Footer = new EmbedFooter(string.Format(this.Appearance.FooterFormat, this.CurrentPage + 1, this.Pages.Count))
            };
        }
    }
}
