//
//  KinkWizard.cs
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
using System.Collections.Generic;
using DIGOS.Ambassador.Discord.Interactivity.Messages;
using DIGOS.Ambassador.Discord.Pagination.Extensions;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Kinks.Wizards
{
    /// <summary>
    /// Acts as an interactive wizard for interactively setting the kink preferences of users.
    /// </summary>
    internal sealed class KinkWizard : InteractiveMessage
    {
        /// <summary>
        /// Gets the emoji used to move the wizard to the next page.
        /// </summary>
        public Emoji Next { get; } = new(default, "\x25B6");

        /// <summary>
        /// Gets the emoji used to move the wizard to the previous page.
        /// </summary>
        public Emoji Previous { get; } = new(default, "\x25C0");

        /// <summary>
        /// Gets the emoji used to move the wizard to the first page.
        /// </summary>
        public Emoji First { get; } = new(default, "\x23EE");

        /// <summary>
        /// Gets the emoji used to move the wizard to the last page.
        /// </summary>
        public Emoji Last { get; } = new(default, "\x23ED");

        /// <summary>
        /// Gets the emoji used to enter a kink category.
        /// </summary>
        public Emoji EnterCategory { get; } = new(default, "\xD83D\xDD22");

        /// <summary>
        /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Favourite"/>.
        /// </summary>
        public Emoji Fave { get; } = new(default, "\x2764");

        /// <summary>
        /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Like"/>.
        /// </summary>
        public Emoji Like { get; } = new(default, "\x2705");

        /// <summary>
        /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Maybe"/>.
        /// </summary>
        public Emoji Maybe { get; } = new(default, "\x26A0");

        /// <summary>
        /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.No"/>.
        /// </summary>
        public Emoji Never { get; } = new(default, "\x26D4");

        /// <summary>
        /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.NoPreference"/>.
        /// </summary>
        public Emoji NoPreference { get; } = new(default, "🤷");

        /// <summary>
        /// Gets the emoji used to move the wizard back out of a category.
        /// </summary>
        public Emoji Back { get; } = new(default, "\x23EB");

        /// <summary>
        /// Gets the emoji used to exit the wizard.
        /// </summary>
        public Emoji Exit { get; } = new(default, "\x23F9");

        /// <summary>
        /// Gets the emoji used to print a help message.
        /// </summary>
        public Emoji Info { get; } = new(default, "\x2139");

        /// <summary>
        /// Gets the names of the reactions, mapped to their emoji.
        /// </summary>
        public IReadOnlyDictionary<string, IEmoji> ReactionNames { get; }

        /// <summary>
        /// Gets the ID of the source user.
        /// </summary>
        public Snowflake SourceUserID { get; }

        /// <summary>
        /// Gets or sets the available categories.
        /// </summary>
        public IReadOnlyList<KinkCategory> Categories { get; internal set; } = new List<KinkCategory>();

        /// <summary>
        /// Gets or sets the internal state of the wizard.
        /// </summary>
        public KinkWizardState State { get; internal set; }

        /// <summary>
        /// Gets or sets the ID of the current kink that's displayed.
        /// </summary>
        public long? CurrentFListKinkID { get; internal set; }

        /// <summary>
        /// Gets the current category offset.
        /// </summary>
        public int CurrentCategoryOffset { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KinkWizard"/> class.
        /// </summary>
        /// <param name="channelID">The ID of the channel the message is in.</param>
        /// <param name="messageID">The ID of the message.</param>
        /// <param name="sourceUserID">The ID of the source user.</param>
        public KinkWizard
        (
            Snowflake channelID,
            Snowflake messageID,
            Snowflake sourceUserID
        )
            : base(channelID, messageID)
        {
            this.SourceUserID = sourceUserID;

            this.State = KinkWizardState.CategorySelection;

            this.ReactionNames = new Dictionary<string, IEmoji>
            {
                { this.Next.GetEmojiName(), this.Next },
                { this.Previous.GetEmojiName(), this.Previous },
                { this.First.GetEmojiName(), this.First },
                { this.Last.GetEmojiName(), this.Last },
                { this.EnterCategory.GetEmojiName(), this.EnterCategory },
                { this.Fave.GetEmojiName(), this.Fave },
                { this.Like.GetEmojiName(), this.Like },
                { this.Maybe.GetEmojiName(), this.Maybe },
                { this.Never.GetEmojiName(), this.Never },
                { this.NoPreference.GetEmojiName(), this.NoPreference },
                { this.Back.GetEmojiName(), this.Back },
                { this.Exit.GetEmojiName(), this.Exit },
                { this.Info.GetEmojiName(), this.Info }
            };
        }

        /// <summary>
        /// Gets the emojis that are associated with the current page.
        /// </summary>
        /// <returns>A set of emojis.</returns>
        public IEnumerable<IEmoji> GetCurrentPageEmotes()
        {
            return this.State switch
            {
                KinkWizardState.CategorySelection => new[] { this.Exit, this.Info, this.First, this.Previous, this.Next, this.Last, this.EnterCategory },
                KinkWizardState.KinkPreference => new[] { this.Exit, this.Info, this.Back, this.Fave, this.Like, this.Maybe, this.Never, this.NoPreference },
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        /// <summary>
        /// Moves the wizard to the next page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MoveNext()
        {
            if (this.CurrentCategoryOffset + 3 >= this.Categories.Count)
            {
                return false;
            }

            this.CurrentCategoryOffset += 3;
            return true;
        }

        /// <summary>
        /// Moves the wizard to the previous page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MovePrevious()
        {
            if (this.CurrentCategoryOffset == 0)
            {
                return false;
            }

            if (this.CurrentCategoryOffset - 3 < 0)
            {
                this.CurrentCategoryOffset = 0;
                return true;
            }

            this.CurrentCategoryOffset -= 3;
            return true;
        }

        /// <summary>
        /// Moves the wizard to the last page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MoveLast()
        {
            int newOffset;
            if (this.Categories.Count % 3 == 0)
            {
                newOffset = this.Categories.Count - 3;
            }
            else
            {
                newOffset = this.Categories.Count - (this.Categories.Count % 3);
            }

            if (newOffset <= this.CurrentCategoryOffset)
            {
                return false;
            }

            this.CurrentCategoryOffset = newOffset;
            return true;
        }

        /// <summary>
        /// Moves the wizard to the first page.
        /// </summary>
        /// <returns>True if the page changed.</returns>
        public bool MoveFirst()
        {
            if (this.CurrentCategoryOffset == 0)
            {
                return false;
            }

            this.CurrentCategoryOffset = 0;
            return true;
        }
    }
}
