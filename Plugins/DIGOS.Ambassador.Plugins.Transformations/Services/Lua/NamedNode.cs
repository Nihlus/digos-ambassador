//
//  NamedNode.cs
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
    /// Represents a named node.
    /// </summary>
    /// <typeparam name="T">The type of value the node holds.</typeparam>
    internal abstract class NamedNode<T> : INode where T : notnull
    {
        /// <inheritdoc />
        public string Name { get; }

        /// <summary>
        /// Gets the value of the node.
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="NamedNode{T}"/> class.
        /// </summary>
        /// <param name="name">The name of the node.</param>
        /// <param name="value">The value of the node.</param>
        protected NamedNode(string name, T value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <inheritdoc />
        public virtual string Format(bool pretty = false)
        {
            return this.Value.ToString() ?? "None";
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Format();
        }
    }
}
