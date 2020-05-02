//
//  PaginatedAppearanceOptions.cs
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

using System;
using Discord;

namespace DIGOS.Ambassador.Discord.Pagination
{
    /// <summary>
    /// represents a set of appearance options for a paginated message.
    /// </summary>
    public class PaginatedAppearanceOptions
    {
        /// <summary>
        /// Gets the default appearance options.
        /// </summary>
        public static PaginatedAppearanceOptions Default => new PaginatedAppearanceOptions();

        /// <summary>
        /// Gets or sets the emote that takes the user back to the first page.
        /// </summary>
        public IEmote First { get; set; } = new Emoji("⏮");

        /// <summary>
        /// Gets or sets the emote that takes the user back one page.
        /// </summary>
        public IEmote Back { get; set; } = new Emoji("◀");

        /// <summary>
        /// Gets or sets the emote that takes the user forward one page.
        /// </summary>
        public IEmote Next { get; set; } = new Emoji("▶");

        /// <summary>
        /// Gets or sets the emote that takes the user to the last page.
        /// </summary>
        public IEmote Last { get; set; } = new Emoji("⏭");

        /// <summary>
        /// Gets or sets the emote that closes the paginated message.
        /// </summary>
        public IEmote Stop { get; set; } = new Emoji("\x23F9");

        /// <summary>
        /// Gets or sets the emote that the user can use to jump to a specific page.
        /// </summary>
        public IEmote Jump { get; set; } = new Emoji("\xD83D\xDD22");

        /// <summary>
        /// Gets or sets the emote that the user can use to display a help message.
        /// </summary>
        public IEmote Help { get; set; } = new Emoji("ℹ");

        /// <summary>
        /// Gets or sets the format string of the footer.
        /// </summary>
        public string FooterFormat { get; set; } = "Page {0}/{1}";

        /// <summary>
        /// Gets or sets the help message.
        /// </summary>
        public string HelpText { get; set; } = "This is a paginator. React with the respective icons to change page.";

        /// <summary>
        /// Gets or sets the condition for displaying the jump message.
        /// </summary>
        public JumpDisplayCondition JumpDisplayCondition { get; set; } = JumpDisplayCondition.WithManageMessages;

        /// <summary>
        /// Gets or sets a value indicating whether or not to display the info emote.
        /// </summary>
        public bool DisplayInformationIcon { get; set; } = true;

        /// <summary>
        /// Gets or sets the timeout for the help message.
        /// </summary>
        public TimeSpan InfoTimeout { get; set; } = TimeSpan.FromSeconds(30.0);

        /// <summary>
        /// Gets or sets the author of the paginated message.
        /// </summary>
        public IUser? Author { get; set; }

        /// <summary>
        /// Gets or sets the colour of the paginated message's embed.
        /// </summary>
        public Color Color { get; set; } = Color.DarkPurple;

        /// <summary>
        /// Gets or sets the title of the paginated message.
        /// </summary>
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Creates a new embed based on the appearance settings.
        /// </summary>
        /// <returns>The embed.</returns>
        public EmbedBuilder CreateEmbedBase()
        {
            var eb = new EmbedBuilder();

            if (!(this.Author is null))
            {
                eb.WithAuthor(this.Author);
            }

            eb.WithColor(this.Color);
            eb.WithFooter(this.FooterFormat);
            eb.WithTitle(this.Title);

            return eb;
        }
    }
}
