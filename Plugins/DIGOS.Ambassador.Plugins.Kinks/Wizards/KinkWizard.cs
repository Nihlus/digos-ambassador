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
using DIGOS.Ambassador.Plugins.Kinks.Model;
using Humanizer;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;

namespace DIGOS.Ambassador.Plugins.Kinks.Wizards;

/// <summary>
/// Acts as an interactive wizard for interactively setting the kink preferences of users.
/// </summary>
internal sealed class KinkWizard : InteractiveMessage
{
    /// <summary>
    /// Gets the emoji used to move the wizard to the next page.
    /// </summary>
    public ButtonComponent Next => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.Next),
        new PartialEmoji(Name: "\x25B6"),
        nameof(this.Next).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard to the previous page.
    /// </summary>
    public ButtonComponent Previous => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.Previous),
        new PartialEmoji(Name: "\x25C0"),
        nameof(this.Previous).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard to the first page.
    /// </summary>
    public ButtonComponent First => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.First),
        new PartialEmoji(Name: "\x23EE"),
        nameof(this.First).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard to the last page.
    /// </summary>
    public ButtonComponent Last => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.Last),
        new PartialEmoji(Name: "\x23ED"),
        nameof(this.Last).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to enter a kink category.
    /// </summary>
    public ButtonComponent EnterCategory => new
    (
        ButtonComponentStyle.Primary,
        nameof(this.EnterCategory).Humanize(),
        new PartialEmoji(Name: "\xD83D\xDD22"),
        nameof(this.EnterCategory).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Favourite"/>.
    /// </summary>
    public ButtonComponent Fave => new
    (
        ButtonComponentStyle.Success,
        nameof(this.Fave),
        new PartialEmoji(Name: "\x2764"),
        nameof(this.Fave).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Like"/>.
    /// </summary>
    public ButtonComponent Like => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.Like),
        new PartialEmoji(Name: "\x2705"),
        nameof(this.Like).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Maybe"/>.
    /// </summary>
    public ButtonComponent Maybe => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.Maybe),
        new PartialEmoji(Name: "\x26A0"),
        nameof(this.Maybe).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.No"/>.
    /// </summary>
    public ButtonComponent Never => new
    (
        ButtonComponentStyle.Danger,
        nameof(this.Never),
        new PartialEmoji(Name: "\x26D4"),
        nameof(this.Never).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.NoPreference"/>.
    /// </summary>
    public ButtonComponent NoPreference => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.NoPreference).Humanize(),
        new PartialEmoji(Name: "🤷"),
        nameof(this.NoPreference).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard back out of a category.
    /// </summary>
    public ButtonComponent Back => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.Back),
        new PartialEmoji(Name: "\x23EB"),
        nameof(this.Back).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to exit the wizard.
    /// </summary>
    public ButtonComponent Exit => new
    (
        ButtonComponentStyle.Secondary,
        nameof(this.Exit),
        new PartialEmoji(Name: "\x23F9"),
        nameof(this.Exit).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to print a help message.
    /// </summary>
    public ButtonComponent Info => new
    (
        ButtonComponentStyle.Primary,
        nameof(this.Info),
        new PartialEmoji(Name: "\x2139"),
        nameof(this.Info).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the buttons that the wizard responds to.
    /// </summary>
    public IReadOnlyList<ButtonComponent> Buttons { get; }

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

        this.Buttons = new List<ButtonComponent>
        {
            this.Next,
            this.Previous,
            this.First,
            this.Last,
            this.EnterCategory,
            this.Fave,
            this.Like,
            this.Maybe,
            this.Never,
            this.NoPreference,
            this.Back,
            this.Exit,
            this.Info
        };
    }

    /// <summary>
    /// Gets the emojis that are associated with the current page.
    /// </summary>
    /// <returns>A set of emojis.</returns>
    public IReadOnlyList<IMessageComponent> GetCurrentPageComponents()
    {
        return this.State switch
        {
            KinkWizardState.CategorySelection => new[]
            {
                new ActionRowComponent
                (
                    new[]
                    {
                        this.First,
                        this.Previous,
                        this.Next,
                        this.Last
                    }
                ),
                new ActionRowComponent
                (
                    new[]
                    {
                        this.Info,
                        this.EnterCategory,
                        this.Exit,
                    }
                )
            },

            KinkWizardState.KinkPreference => new[]
            {
                new ActionRowComponent
                (
                    new[]
                    {
                        this.Fave,
                        this.Like,
                        this.Maybe,
                        this.Never,
                        this.NoPreference
                    }
                ),
                new ActionRowComponent
                (
                    new[]
                    {
                        this.Info,
                        this.Back,
                        this.Exit,
                    }
                )
            },

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
