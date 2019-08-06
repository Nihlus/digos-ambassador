//
//  ExportFormatExtensions.cs
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

using System;
using DIGOS.Ambassador.Plugins.Roleplaying.Services.Exporters;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Extensions
{
    /// <summary>
    /// Holds extension methods for <see cref="ExportFormat"/>.
    /// </summary>
    public static class ExportFormatExtensions
    {
        /// <summary>
        /// Gets the file extension for the given export format.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <returns>The extension.</returns>
        /// <exception cref="NotImplementedException">Thrown if the format has not been implemented.</exception>
        [NotNull]
        public static string GetFileExtension(this ExportFormat format)
        {
            switch (format)
            {
                case ExportFormat.PDF:
                {
                    return "pdf";
                }
                case ExportFormat.Plaintext:
                {
                    return "txt";
                }
                case ExportFormat.JSON:
                {
                    return "json";
                }
                case ExportFormat.ODT:
                {
                    return "odt";
                }
                default:
                {
                    throw new NotImplementedException();
                }
            }
        }
    }
}
