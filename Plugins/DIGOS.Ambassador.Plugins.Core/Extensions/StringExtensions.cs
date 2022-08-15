//
//  StringExtensions.cs
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
    /// <param name="value">The base value.</param>
    /// <param name="length">The maximum length.</param>
    /// <returns>The ellipsized string.</returns>
    [return: NotNullIfNotNull("value")]
    public static string? Ellipsize(this string? value, int length)
    {
        if (value is null)
        {
            return null;
        }

        return value.Length <= length ? value : value[..(length - 1)] + (char)0x2026;
    }
}
