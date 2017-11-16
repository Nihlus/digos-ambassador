﻿//
//  Dossier.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

namespace DIGOS.Ambassador.Database.Dossiers
{
	/// <summary>
	/// Represents a dossier entry.
	/// </summary>
	public class Dossier
	{
		/// <summary>
		/// Gets or sets the unique dossier ID.
		/// </summary>
		public uint DossierID { get; set; }

		/// <summary>
		/// Gets or sets the title of the dossier.
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Gets or sets the summary of the dossier.
		/// </summary>
		public string Summary { get; set; }

		/// <summary>
		/// Gets or sets the absolute file path to the dossier.
		/// </summary>
		public string Path { get; set; }
	}
}