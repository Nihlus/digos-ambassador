//
//  StringExtensions.cs
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

using System.Diagnostics.CodeAnalysis;

namespace DIGOS.Ambassador.Plugins.Core.Extensions;

/// <summary>
/// Contains extension methods for the string class.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Ellipsizes the given string, truncating it with the ellipsis character if required.
    /// </summary>
    /// <param name="this">The base value.</param>
    /// <param name="length">The maximum length.</param>
    /// <returns>The ellipsized string.</returns>
    [return: NotNullIfNotNull("this")]
    public static string? Ellipsize(this string? @this, int length)
    {
        if (@this is null)
        {
            return null;
        }

        return @this.Length <= length ? @this : @this.Substring(0, length - 1) + (char)0x2026;
    }
}
