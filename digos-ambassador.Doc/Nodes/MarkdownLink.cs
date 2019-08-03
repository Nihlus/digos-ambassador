//
//  MarkdownLink.cs
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

using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Extensions;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Doc.Nodes
{
    /// <summary>
    /// Represents a link.
    /// </summary>
    public class MarkdownLink : IMarkdownNode
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MarkdownLink"/> class.
        /// </summary>
        /// <param name="destination">The link destination.</param>
        /// <param name="text">The link text.</param>
        public MarkdownLink(string destination, string text)
        {
            this.Destination = destination;
            this.Text = text;
        }

        /// <summary>
        /// Gets or sets the link destination.
        /// </summary>
        public string Destination { get; set; }

        /// <summary>
        /// Gets or sets the visible text of the link.
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets the link's hover tooltip.
        /// </summary>
        public string Tooltip { get; set; }

        /// <inheritdoc />
        [NotNull]
        public virtual string Compile()
        {
            if (this.Tooltip.IsNullOrWhitespace())
            {
                return $"[{this.Text}]({this.Destination})";
            }

            return $"[{this.Text}]({this.Destination} \"{this.Tooltip}\")";
        }
    }
}
