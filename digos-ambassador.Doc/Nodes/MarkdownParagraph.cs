//
//  MarkdownParagraph.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Doc.Nodes
{
    /// <summary>
    /// Represents a paragraph of text.
    /// </summary>
    public class MarkdownParagraph : IMarkdownNode
    {
        /// <summary>
        /// Gets a list of the text compontents in the paragraph.
        /// </summary>
        public List<MarkdownText> Components { get; } = new List<MarkdownText>();

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownParagraph"/> class.
        /// </summary>
        public MarkdownParagraph()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownParagraph"/> class.
        /// </summary>
        /// <param name="text">The text in the paragraph.</param>
        public MarkdownParagraph(string text)
        {
            AppendText(text);
        }

        /// <inheritdoc />
        [NotNull]
        public string Compile()
        {
            return $"{this.Components.Select(c => c.Compile()).Aggregate((a, b) => a + b)}";
        }

        /// <summary>
        /// Appends a piece of text to the paragraph.
        /// </summary>
        /// <param name="text">The text to append.</param>
        /// <returns>The paragraph, with the text appended.</returns>
        [NotNull]
        public MarkdownParagraph AppendText(MarkdownText text)
        {
            this.Components.Add(text);
            return this;
        }

        /// <summary>
        /// Appends a piece of text to the paragraph.
        /// </summary>
        /// <param name="text">The text to append.</param>
        /// <returns>The paragraph, with the text appended.</returns>
        [NotNull]
        public MarkdownParagraph AppendText(string text)
        {
            return AppendText(new MarkdownText(text));
        }

        /// <summary>
        /// Appends a piece of text, followed by a line break.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The paragrpah, with the text appended.</returns>
        [NotNull]
        public MarkdownParagraph AppendLine(MarkdownText text)
        {
            return AppendText(text).AppendText("\n\n");
        }

        /// <summary>
        /// Appends a piece of text, followed by a line break.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns>The paragrpah, with the text appended.</returns>
        [NotNull]
        public MarkdownParagraph AppendLine(string text)
        {
            return AppendText(text).AppendText("\n\n");
        }
    }
}
