﻿//
//  MetaTableBuilder.cs
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

namespace DIGOS.Ambassador.Plugins.Transformations.Services.Lua
{
    /// <summary>
    /// Builds a lua metatable from a list of values and functions names.
    /// </summary>
    public sealed class MetaTableBuilder
    {
        private readonly List<string> _entries = new List<string>();

        /// <summary>
        /// Adds a unique entry to the builder.
        /// </summary>
        /// <param name="entry">The entry.</param>
        /// <returns>The builder with the entry.</returns>
        public MetaTableBuilder WithEntry(string entry)
        {
            if (!_entries.Contains(entry))
            {
                _entries.Add(entry);
            }

            return this;
        }

        /// <summary>
        /// Builds the metatable.
        /// </summary>
        /// <param name="pretty">Whether or not the output should be in a pretty format.</param>
        /// <returns>The metatable as a formatted string.</returns>
        public string Build(bool pretty = false)
        {
            var metatable = new TableNode("env", new List<INode>());

            foreach (var entry in _entries)
            {
                PopulateSubNodes(metatable, entry, entry);
            }

            return metatable.Format(pretty);
        }

        private void PopulateSubNodes
        (
            TableNode parent,
            string value,
            string originalValue
        )
        {
            var components = value.Split('.');
            if (components.Length == 1)
            {
                var valueNode = new ValueNode<string>(value, originalValue);

                parent.Value.Add(valueNode);
                return;
            }

            var subnode = parent.Value.FirstOrDefault(t => t.Name == components.First());
            if (subnode is null)
            {
                subnode = new TableNode(components.First(), new List<INode>());
                parent.Value.Add(subnode);
            }

            if (subnode is TableNode subtable)
            {
                PopulateSubNodes(subtable, string.Join(".", components.Skip(1)), originalValue);
            }
        }
    }
}
