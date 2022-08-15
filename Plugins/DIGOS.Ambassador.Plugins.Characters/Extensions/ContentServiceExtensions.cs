//
//  ContentServiceExtensions.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Core.Services;

namespace DIGOS.Ambassador.Plugins.Characters.Extensions;

/// <summary>
/// Extension methods for the <see cref="ContentService"/> class.
/// </summary>
public static class ContentServiceExtensions
{
    private static string DefaultAvatar => "avatars/Discord_DIGOS.png";

    /// <summary>
    /// Gets the default avatar URI for characters.
    /// </summary>
    /// <param name="this">The content service.</param>
    /// <returns>The default avatar URI.</returns>
    public static Uri GetDefaultAvatarUri(this ContentService @this)
    {
        return new Uri(@this.BaseCDNUri, $"plugins/characters/{DefaultAvatar}");
    }
}
