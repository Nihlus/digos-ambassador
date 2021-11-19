//
//  InteractivityServiceExtensions.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity;
using Remora.Discord.API.Objects;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Discord.Pagination.Extensions;

/// <summary>
/// Defines extension methods for the <see cref="InteractivityService"/> class.
/// </summary>
public static class InteractivityServiceExtensions
{
    /// <summary>
    /// Sends an interactive message.
    /// </summary>
    /// <param name="interactivityService">The interactivity service.</param>
    /// <param name="sourceUser">The source user.</param>
    /// <param name="pages">The pages to send.</param>
    /// <param name="appearanceOptions">Custom appearance options, if any.</param>
    /// <param name="ct">The cancellation token for this operation.</param>
    /// <returns>A result which may or may not have succeeded.</returns>
    public static Task<Result> SendContextualInteractiveMessageAsync
    (
        this InteractivityService interactivityService,
        Snowflake sourceUser,
        IReadOnlyList<Embed> pages,
        PaginatedAppearanceOptions? appearanceOptions = default,
        CancellationToken ct = default
    )
    {
        return interactivityService.SendContextualInteractiveMessageAsync
        (
            (c, m) => new PaginatedMessage(c, m, sourceUser, pages, appearanceOptions),
            ct
        );
    }
}
