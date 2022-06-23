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
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Kinks.Model;
using DIGOS.Ambassador.Plugins.Kinks.Services;
using Humanizer;
using Remora.Commands.Results;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Rest.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Kinks.Wizards;

/// <summary>
/// Acts as an interactive wizard for interactively setting the kink preferences of users.
/// </summary>
public sealed class KinkWizard
{
    /// <summary>
    /// Gets the emoji used to move the wizard to the next page.
    /// </summary>
    public ButtonComponent Next { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(Next),
        new PartialEmoji(Name: "\x25B6"),
        nameof(Next).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard to the previous page.
    /// </summary>
    public ButtonComponent Previous { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(Previous),
        new PartialEmoji(Name: "\x25C0"),
        nameof(Previous).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard to the first page.
    /// </summary>
    public ButtonComponent First { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(First),
        new PartialEmoji(Name: "\x23EE"),
        nameof(First).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard to the last page.
    /// </summary>
    public ButtonComponent Last { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(Last),
        new PartialEmoji(Name: "\x23ED"),
        nameof(Last).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to enter a kink category.
    /// </summary>
    public ButtonComponent EnterCategory { get; } = new
    (
        ButtonComponentStyle.Primary,
        nameof(EnterCategory).Humanize(),
        new PartialEmoji(Name: "\xD83D\xDD22"),
        nameof(EnterCategory).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Favourite"/>.
    /// </summary>
    public ButtonComponent Favourite { get; } = new
    (
        ButtonComponentStyle.Success,
        nameof(Favourite),
        new PartialEmoji(Name: "\x2764"),
        nameof(Favourite).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Like"/>.
    /// </summary>
    public ButtonComponent Like { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(Like),
        new PartialEmoji(Name: "\x2705"),
        nameof(Like).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.Maybe"/>.
    /// </summary>
    public ButtonComponent Maybe { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(Maybe),
        new PartialEmoji(Name: "\x26A0"),
        nameof(Maybe).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.No"/>.
    /// </summary>
    public ButtonComponent No { get; } = new
    (
        ButtonComponentStyle.Danger,
        nameof(No),
        new PartialEmoji(Name: "\x26D4"),
        nameof(No).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to set a kink's preference to <see cref="KinkPreference.NoPreference"/>.
    /// </summary>
    public ButtonComponent NoPreference { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(NoPreference).Humanize(),
        new PartialEmoji(Name: "🤷"),
        nameof(NoPreference).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to move the wizard back out of a category.
    /// </summary>
    public ButtonComponent Back { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(Back),
        new PartialEmoji(Name: "\x23EB"),
        nameof(Back).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to exit the wizard.
    /// </summary>
    public ButtonComponent Exit { get; } = new
    (
        ButtonComponentStyle.Secondary,
        nameof(Exit),
        new PartialEmoji(Name: "\x23F9"),
        nameof(Exit).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the emoji used to print a help message.
    /// </summary>
    public ButtonComponent Info { get; } = new
    (
        ButtonComponentStyle.Primary,
        nameof(Info),
        new PartialEmoji(Name: "\x2139"),
        nameof(Info).ToLowerInvariant()
    );

    /// <summary>
    /// Gets the buttons that the wizard responds to.
    /// </summary>
    public IReadOnlyList<ButtonComponent> Buttons { get; }

    /// <summary>
    /// Gets the ID of the channel the message was sent in.
    /// </summary>
    public Snowflake ChannelID { get; }

    /// <summary>
    /// Gets the ID of the source user.
    /// </summary>
    public Snowflake SourceUserID { get; }

    /// <summary>
    /// Gets the available categories.
    /// </summary>
    public IReadOnlyList<KinkCategory> Categories { get; }

    /// <summary>
    /// Gets the internal state of the wizard.
    /// </summary>
    public KinkWizardState State { get; private set; }

    /// <summary>
    /// Gets the ID of the current kink that's displayed.
    /// </summary>
    public long? CurrentFListKinkID { get; private set; }

    /// <summary>
    /// Gets the current category offset.
    /// </summary>
    public int CurrentCategoryOffset { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="KinkWizard"/> class.
    /// </summary>
    /// <param name="channelID">The ID of the channel the message is in.</param>
    /// <param name="sourceUserID">The ID of the source user.</param>
    /// <param name="categories">The available kink categories.</param>
    public KinkWizard
    (
        Snowflake channelID,
        Snowflake sourceUserID,
        IReadOnlyList<KinkCategory> categories
    )
    {
        this.ChannelID = channelID;
        this.SourceUserID = sourceUserID;
        this.Categories = categories;

        this.State = KinkWizardState.CategorySelection;

        this.Buttons = new List<ButtonComponent>
        {
            this.Next,
            this.Previous,
            this.First,
            this.Last,
            this.EnterCategory,
            this.Favourite,
            this.Like,
            this.Maybe,
            this.No,
            this.NoPreference,
            this.Back,
            this.Exit,
            this.Info
        };
    }

    /// <summary>
    /// Returns the wizard to the category selection.
    /// </summary>
    public void GoToCategorySelection()
    {
        if (this.State is KinkWizardState.CategorySelection)
        {
            throw new InvalidOperationException("The wizard is already at the category selection.");
        }

        this.CurrentFListKinkID = null;
        this.State = KinkWizardState.CategorySelection;
    }

    /// <summary>
    /// Advances the wizard to the next kink in the category.
    /// </summary>
    /// <param name="kinks">The kink service.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>true if there are more kinks in the category; otherwise, false.</returns>
    public async Task<Result<bool>> MoveToNextKinkInCategoryAsync(KinkService kinks, CancellationToken ct)
    {
        if (this.CurrentFListKinkID is null)
        {
            throw new InvalidOperationException();
        }

        var getNextKinkResult = await kinks.GetNextKinkByCurrentFListIDAsync(this.CurrentFListKinkID.Value, ct);
        if (!getNextKinkResult.IsSuccess)
        {
            return Result<bool>.FromError(getNextKinkResult);
        }

        var nextKink = getNextKinkResult.Entity;
        this.CurrentFListKinkID = nextKink?.FListID;

        return nextKink is not null;
    }

    /// <summary>
    /// Opens the category of the given name.
    /// </summary>
    /// <param name="kinks">The kink service.</param>
    /// <param name="categoryName">The category name.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    public async Task<Result> OpenCategoryAsync(KinkService kinks, string categoryName, CancellationToken ct = default)
    {
        if (this.State is not KinkWizardState.CategorySelection)
        {
            throw new InvalidOperationException("The wizard is not at the category selection.");
        }

        var getCategoryResult = this.Categories.Select(c => c.ToString()).BestLevenshteinMatch(categoryName, 0.75);
        if (!getCategoryResult.IsSuccess)
        {
            return (Result)getCategoryResult;
        }

        if (!Enum.TryParse<KinkCategory>(getCategoryResult.Entity, true, out var category))
        {
            return new ParsingError<KinkCategory>("Could not parse kink category.");
        }

        var getKinkResult = await kinks.GetFirstKinkWithoutPreferenceInCategoryAsync
        (
            this.SourceUserID,
            category,
            ct
        );

        if (!getKinkResult.IsSuccess)
        {
            return (Result)getKinkResult;
        }

        Kink kink;
        if (getKinkResult.Entity is not null)
        {
            kink = getKinkResult.Entity;
        }
        else
        {
            var getFirstKinkResult = await kinks.GetFirstKinkInCategoryAsync(category, ct);
            if (!getFirstKinkResult.IsSuccess)
            {
                return (Result)getFirstKinkResult;
            }

            kink = getFirstKinkResult.Entity;
        }

        this.CurrentFListKinkID = kink.FListID;
        this.State = KinkWizardState.KinkPreference;

        return Result.FromSuccess();
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
                        this.Favourite,
                        this.Like,
                        this.Maybe,
                        this.No,
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
    /// Gets an embed that represents the current page.
    /// </summary>
    /// <param name="kinks">The kink service.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>The embed.</returns>
    public async Task<Result<Embed>> GetCurrentPageAsync
    (
        KinkService kinks,
        CancellationToken ct = default
    )
    {
        switch (this.State)
        {
            case KinkWizardState.CategorySelection:
            {
                var eb = new Embed
                {
                    Title = "Category selection",
                    Colour = Color.MediumPurple
                };

                if (this.Categories.Any())
                {
                    var visibleCategories = this.Categories.Skip(this.CurrentCategoryOffset).Take(3).ToList();
                    var visibleCategoryFields = visibleCategories.Select
                        (
                            c => new EmbedField(c.ToString().Humanize().Transform(To.TitleCase), c.Humanize())
                        )
                        .ToList();

                    var offset = this.CurrentCategoryOffset + 1;
                    eb = eb with
                    {
                        Description = "Select from one of the categories below.",
                        Fields = visibleCategoryFields,
                        Footer = new EmbedFooter
                        (
                            $"Categories {offset}-{offset + visibleCategories.Count - 1} / "
                            + $"{this.Categories.Count}"
                        )
                    };
                }
                else
                {
                    eb = eb with
                    {
                        Description = "There aren't any categories in the database."
                    };
                }

                return eb;
            }
            case KinkWizardState.KinkPreference:
            {
                if (this.CurrentFListKinkID is null)
                {
                    throw new InvalidOperationException();
                }

                var getUserKinkResult = await kinks.GetUserKinkByFListIDAsync
                (
                    this.SourceUserID,
                    this.CurrentFListKinkID.Value,
                    ct
                );

                if (!getUserKinkResult.IsSuccess)
                {
                    return Result<Embed>.FromError(getUserKinkResult);
                }

                var userKink = getUserKinkResult.Entity;
                return kinks.BuildUserKinkInfoEmbedBase(userKink);
            }
            default:
            {
                throw new ArgumentOutOfRangeException();
            }
        }
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
