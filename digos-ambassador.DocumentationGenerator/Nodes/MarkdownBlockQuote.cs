//
//  MarkdownBlockQuote.cs
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

using System.Text;

namespace DIGOS.Ambassador.Doc.Nodes
{
	/// <summary>
	/// Represents a markdown block quote.
	/// </summary>
	public class MarkdownBlockQuote : IMarkdownNode
	{
		/// <summary>
		/// Gets or sets the quoted content.
		/// </summary>
		public IMarkdownNode Content { get; set; }

		/// <inheritdoc />
		public string Compile()
		{
			var sb = new StringBuilder();
			foreach (var line in this.Content.Compile().Split('\n'))
			{
				sb.AppendLine($"> {line}");
			}

			return sb.ToString();
		}
	}
}
