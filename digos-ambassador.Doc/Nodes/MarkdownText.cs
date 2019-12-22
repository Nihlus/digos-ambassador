﻿//
//  MarkdownText.cs
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
    /// Represents text in a markdown document.
    /// </summary>
    public class MarkdownText : IMarkdownNode
    {
        /// <summary>
        /// Gets the contents of the text.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets or sets the type of emphasis placed on the text.
        /// </summary>
        public EmphasisType Emphasis { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownText"/> class.
        /// </summary>
        /// <param name="text">The text.</param>
        public MarkdownText(string text)
        {
            this.Content = text;
        }

        /// <inheritdoc />
        public string Compile()
        {
            var content = this.Content;

            if (this.Emphasis.HasFlag(EmphasisType.Italic))
            {
                content = $"*{content}*";
            }

            if (this.Emphasis.HasFlag(EmphasisType.Bold))
            {
                content = $"**{content}**";
            }

            if (this.Emphasis.HasFlag(EmphasisType.Strikethrough))
            {
                content = $"~~{content}~~";
            }

            return content;
        }
    }
}
