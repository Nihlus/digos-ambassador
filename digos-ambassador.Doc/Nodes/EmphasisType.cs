//
//  EmphasisType.cs
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

namespace DIGOS.Ambassador.Doc.Nodes
{
	/// <summary>
	/// Represents emphasis types for markdown text.
	/// </summary>
	[Flags]
	public enum EmphasisType
	{
		/// <summary>
		/// Italic (cursive) text.
		/// </summary>
		Italic = 0x1,

		/// <summary>
		/// Bold text.
		/// </summary>
		Bold = 0x2,

		/// <summary>
		/// Struck text.
		/// </summary>
		Strikethrough = 0x4
	}
}
