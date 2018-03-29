//
//  MarkdownHeader.cs
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

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Doc.Nodes
{
	/// <summary>
	/// Represents a markdown header.
	/// </summary>
	public class MarkdownHeader : IMarkdownNode
	{
		/// <summary>
		/// Gets or sets the level of the header.
		/// </summary>
		public int Level { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the header should be underlined. Only affects levels 1 and 2.
		/// </summary>
		public bool Underline { get; set; }

		/// <summary>
		/// Gets or sets the title text of the header.
		/// </summary>
		[NotNull]
		public MarkdownText Title { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="MarkdownHeader"/> class.
		/// </summary>
		/// <param name="title">The title.</param>
		/// <param name="level">The level.</param>
		/// <param name="underline">Whether or not the header should be underlined.</param>
		public MarkdownHeader(string title, int level, bool underline = false)
		{
			this.Title = new MarkdownText(title);
			this.Level = level;
			this.Underline = underline;
		}

		/// <inheritdoc />
		[NotNull]
		public string Compile()
		{
			if (this.Underline && this.Level <= 2)
			{
				switch (this.Level)
				{
					case 1:
					{
						return $"{this.Title.Compile()}\n{new string('=', this.Title.Compile().Length)}";
					}
					case 2:
					{
						return $"{this.Title.Compile()}\n{new string('-', this.Title.Compile().Length)}";
					}
				}
			}

			return $"{new string('#', this.Level)} {this.Title.Compile()}";
		}
	}
}
