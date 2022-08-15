//
//  TableNode.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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

namespace DIGOS.Ambassador.Plugins.Transformations.Services.Lua;

/// <summary>
/// Represents a named table, which holds a list of nodes.
/// </summary>
internal sealed class TableNode : NamedNode<List<INode>>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TableNode"/> class.
    /// </summary>
    /// <param name="name">The name of the table.</param>
    /// <param name="value">The nodes in the table.</param>
    public TableNode(string name, List<INode> value)
        : base(name, value)
    {
    }

    /// <inheritdoc />
    public override string Format(bool pretty = false)
    {
        var separator = ",";
        if (pretty)
        {
            separator = ",\n";
        }

        return $"{this.Name} = {{ {string.Join(separator, this.Value.Select(n => n.Format()))} }}";
    }
}
