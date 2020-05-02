//
//  Dossier.cs
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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DIGOS.Ambassador.Core.Database.Entities;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Dossiers.Model
{
    /// <summary>
    /// Represents a dossier entry.
    /// </summary>
    [PublicAPI]
    [Table("Dossiers", Schema = "DossierModule")]
    public class Dossier : EFEntity
    {
        /// <summary>
        /// Gets the title of the dossier.
        /// </summary>
        [Required]
        public string Title { get; internal set; }

        /// <summary>
        /// Gets the summary of the dossier.
        /// </summary>
        [Required]
        public string Summary { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dossier"/> class.
        /// </summary>
        /// <param name="title">The title of the dossier.</param>
        /// <param name="summary">The dossier's summary.</param>
        public Dossier(string title, string summary = "No summary set.")
        {
            this.Title = title;
            this.Summary = summary;
        }
    }
}
