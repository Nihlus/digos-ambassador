//
//  ExportedRoleplay.cs
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
using System.IO;

namespace DIGOS.Ambassador.Services.Exporters
{
    /// <summary>
    /// Wraps the exported data from a roleplay.
    /// </summary>
    public class ExportedRoleplay : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportedRoleplay"/> class.
        /// </summary>
        /// <param name="title">The title of the roleplay.</param>
        /// <param name="format">The format of the exported data.</param>
        /// <param name="data">The exported data.</param>
        public ExportedRoleplay(string title, ExportFormat format, Stream data)
        {
            this.Title = title;
            this.Format = format;
            this.Data = data;
        }

        /// <summary>
        /// Gets the title of the roleplay. This is often used for the output file name.
        /// </summary>
        public string Title { get; }

        /// <summary>
        /// Gets the exported format of the roleplay.
        /// </summary>
        public ExportFormat Format { get; }

        /// <summary>
        /// Gets the stream that contains the data in the roleplay.
        /// </summary>
        public Stream Data { get; }

        /// <inheritdoc />
        public void Dispose()
        {
            this.Data?.Dispose();
        }
    }
}
