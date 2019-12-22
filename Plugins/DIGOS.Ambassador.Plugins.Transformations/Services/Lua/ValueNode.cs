//
//  ValueNode.cs
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

namespace DIGOS.Ambassador.Plugins.Transformations.Services.Lua
{
    /// <summary>
    /// Represents a named node that holds a value.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    internal sealed class ValueNode<T> : NamedNode<T> where T : notnull
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ValueNode{T}"/> class.
        /// </summary>
        /// <param name="name">The node's name.</param>
        /// <param name="originalValue">The original value of the node.</param>
        public ValueNode(string name, T originalValue)
            : base(name, originalValue)
        {
        }

        /// <inheritdoc />
        public override string Format(bool pretty = false)
        {
            return $"{this.Name} = {this.Value.ToString() ?? "None"}";
        }
    }
}
