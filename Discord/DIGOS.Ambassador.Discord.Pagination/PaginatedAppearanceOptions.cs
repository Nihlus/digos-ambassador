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

using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;

namespace DIGOS.Ambassador.Discord.Pagination
{
    /// <summary>
    /// Represents a set of appearance options for a paginated message.
    /// </summary>
    public sealed record PaginatedAppearanceOptions
    (
        ButtonComponent First,
        ButtonComponent Back,
        ButtonComponent Next,
        ButtonComponent Last,
        ButtonComponent Close,
        ButtonComponent Help,
        string FooterFormat = "Page {0}/{1}",
        string HelpText = "This is a paginated message. React with the respective icons to change page."
    )
    {
        /// <summary>
        /// Holds the default appearance instance.
        /// </summary>
        public static readonly PaginatedAppearanceOptions Default = new
        (
            new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                nameof(First),
                new PartialEmoji(Name: "⏮"),
                nameof(First)
            ),
            new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                nameof(Back),
                new PartialEmoji(Name: "◀"),
                nameof(Back)
            ),
            new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                nameof(Next),
                new PartialEmoji(Name: "▶"),
                nameof(Next)
            ),
            new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                nameof(Last),
                new PartialEmoji(Name: "⏭"),
                nameof(Last)
            ),
            new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                nameof(Close),
                new PartialEmoji(Name: "\x23F9"),
                nameof(Close)
            ),
            new ButtonComponent
            (
                ButtonComponentStyle.Secondary,
                nameof(Help),
                new PartialEmoji(Name: "ℹ"),
                nameof(Help)
            )
        );
    }
}
