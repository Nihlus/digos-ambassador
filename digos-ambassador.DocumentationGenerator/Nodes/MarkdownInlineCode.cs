//
//  MarkdownInlineCode.cs
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

namespace DIGOS.Ambassador.Doc.Nodes
{
	/// <summary>
	/// Represents inline code formatting.
	/// </summary>
	public class MarkdownInlineCode : IMarkdownNode
	{
		/// <summary>
		/// Gets or sets the inlined content.
		/// </summary>
		public IMarkdownNode Content { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MarkdownInlineCode"/> class.
		/// </summary>
		/// <param name="content">The text to inline.</param>
		public MarkdownInlineCode(string content)
		{
			this.Content = new MarkdownText(content);
		}

		/// <inheritdoc />
		public string Compile()
		{
			return $"`{this.Content.Compile()}`";
		}
	}
}
