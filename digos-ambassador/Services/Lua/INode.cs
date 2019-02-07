//
//  INode.cs
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

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Represents a node in a lua table tree structure.
    /// </summary>
    public interface INode
    {
        /// <summary>
        /// Gets or sets the name of the node.
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Formats the node into a lua string.
        /// </summary>
        /// <param name="pretty">Whether or not the format should be in a pretty format.</param>
        /// <returns>A lua string.</returns>
        string Format(bool pretty = false);
    }
}
