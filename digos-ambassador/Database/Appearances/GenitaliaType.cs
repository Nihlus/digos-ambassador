//
//  GenitaliaType.cs
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

using System;

namespace DIGOS.Ambassador.Database.Appearances
{
	/// <summary>
	/// Represents different types of genitalia a character can have.
	/// </summary>
	[Flags]
	public enum GenitaliaType
	{
		/// <summary>
		/// A male penis.
		/// </summary>
		Penis,

		/// <summary>
		/// A female vagina.
		/// </summary>
		Vagina,

		/// <summary>
		/// An egg-laying ovipositor.
		/// </summary>
		Ovipositor
	}
}
