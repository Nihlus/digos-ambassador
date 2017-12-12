//
//  MarkdownList.cs
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
using System.Linq;
using System.Text;

namespace DIGOS.Ambassador.Doc.Nodes
{
	/// <summary>
	/// Represents a list of items.
	/// </summary>
	public class MarkdownList : IMarkdownNode
	{
		/// <summary>
		/// Gets the item in the list.
		/// </summary>
		public List<IMarkdownNode> Items { get; } = new List<IMarkdownNode>();

		/// <summary>
		/// Gets or sets the type of list this is.
		/// </summary>
		public ListType Type { get; set; }

		/// <inheritdoc />
		public string Compile()
		{
			var sb = new StringBuilder();
			var itemNumber = 1;
			foreach (var item in this.Items)
			{
				var itemLines = item.Compile().Split('\n');
				switch (this.Type)
				{
					case ListType.Numbered:
					{
						sb.AppendLine($"{itemNumber}. {itemLines.First()}");
						break;
					}
					case ListType.Bullet:
					{
						sb.AppendLine($"* {itemLines.First()}");
						break;
					}
				}

				foreach (var line in itemLines.Skip(1))
				{
					switch (this.Type)
					{
						case ListType.Numbered:
						{
							sb.AppendLine($"{new string(' ', $"{itemNumber}.".Length)} {line}");
							break;
						}
						case ListType.Bullet:
						{
							sb.AppendLine($"{new string(' ', "*".Length)} {line}");
							break;
						}
					}
				}

				++itemNumber;
			}

			return sb.ToString();
		}

		/// <summary>
		/// Appends a new item to the list.
		/// </summary>
		/// <param name="item">The item to append.</param>
		/// <returns>The list, with the item appended.</returns>
		public MarkdownList AppendItem(IMarkdownNode item)
		{
			this.Items.Add(item);
			return this;
		}
	}
}
