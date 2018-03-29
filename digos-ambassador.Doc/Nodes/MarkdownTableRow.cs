//
//  MarkdownTableRow.cs
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

using System.Collections.Generic;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Doc.Nodes
{
	/// <summary>
	/// Represents a row in a table.
	/// </summary>
	public class MarkdownTableRow
	{
		/// <summary>
		/// Gets a list of the cells in the row.
		/// </summary>
		public List<IMarkdownNode> Cells { get; } = new List<IMarkdownNode>();

		/// <summary>
		/// Appends a new cell to the row.
		/// </summary>
		/// <param name="cell">The cell to append.</param>
		/// <returns>The row, with the cell appended.</returns>
		[NotNull]
		public MarkdownTableRow AppendCell(IMarkdownNode cell)
		{
			this.Cells.Add(cell);
			return this;
		}
	}
}
