//
//  PermissionExtensions.cs
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

using System.Text;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions.Extensions;

/// <summary>
/// Extension methods for the <see cref="IPermission"/> interface and <see cref="Permission"/> class.
/// </summary>
internal static class PermissionExtensions
{
    /// <summary>
    /// Formats the data in the permission as a title, including the allowed targets.
    /// </summary>
    /// <param name="permission">The permission to format.</param>
    /// <returns>The formatted title.</returns>
    [Pure]
    public static string FormatTitle(this IPermission permission)
    {
        if (!permission.IsGrantedByDefaultToSelf && !permission.IsGrantedByDefaultToOthers)
        {
            return permission.FriendlyName;
        }

        var extraInfo = new StringBuilder();
        extraInfo.Append('(');
        extraInfo.Append("Granted by default, targeting ");

        if (permission.IsGrantedByDefaultToSelf && permission.IsGrantedByDefaultToOthers)
        {
            extraInfo.Append("yourself and others.");
        }
        else
        {
            if (permission.IsGrantedByDefaultToSelf)
            {
                extraInfo.Append("yourself");
            }

            if (permission.IsGrantedByDefaultToOthers)
            {
                extraInfo.Append("others");
            }
        }

        extraInfo.Append(')');

        return $"{permission.FriendlyName} {extraInfo}";
    }
}
