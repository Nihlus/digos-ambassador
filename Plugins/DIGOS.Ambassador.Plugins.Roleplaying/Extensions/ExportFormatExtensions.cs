//
//  ExportFormatExtensions.cs
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
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Extensions;

/// <summary>
/// Holds extension methods for <see cref="ExportFormat"/>.
/// </summary>
internal static class ExportFormatExtensions
{
    /// <summary>
    /// Gets the file extension for the given export format.
    /// </summary>
    /// <param name="format">The format.</param>
    /// <returns>The extension.</returns>
    /// <exception cref="NotImplementedException">Thrown if the format has not been implemented.</exception>
    public static string GetFileExtension(this ExportFormat format)
    {
        return format switch
        {
            ExportFormat.PDF => "pdf",
            ExportFormat.Plaintext => "txt",
            ExportFormat.JSON => "json",
            ExportFormat.ODT => "odt",
            _ => throw new NotImplementedException()
        };
    }
}
