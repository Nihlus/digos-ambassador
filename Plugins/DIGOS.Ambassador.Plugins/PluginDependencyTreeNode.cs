//
//  PluginDependencyTreeNode.cs
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
using DIGOS.Ambassador.Plugins.Abstractions;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins
{
    /// <summary>
    /// Represents a node in a dependency tree.
    /// </summary>
    [PublicAPI]
    public class PluginDependencyTreeNode
    {
        [NotNull, ItemNotNull]
        private readonly List<PluginDependencyTreeNode> _dependants;

        /// <summary>
        /// Gets the plugin.
        /// </summary>
        [NotNull]
        public IPluginDescriptor Plugin { get; }

        /// <summary>
        /// Gets the nodes that depend on this plugin.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<PluginDependencyTreeNode> Dependants => _dependants;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginDependencyTreeNode"/> class.
        /// </summary>
        /// <param name="plugin">The plugin.</param>
        /// <param name="dependants">The dependants.</param>
        public PluginDependencyTreeNode
        (
            [NotNull] IPluginDescriptor plugin,
            [CanBeNull, ItemNotNull] List<PluginDependencyTreeNode> dependants = null
        )
        {
            this.Plugin = plugin;
            _dependants = dependants ?? new List<PluginDependencyTreeNode>();
        }

        /// <summary>
        /// Adds a dependant to this node.
        /// </summary>
        /// <param name="node">The node.</param>
        internal void AddDependant([NotNull] PluginDependencyTreeNode node)
        {
            if (_dependants.Contains(node))
            {
                return;
            }

            _dependants.Add(node);
        }

        /// <summary>
        /// Gets all the dependant plugins in this branch.
        /// </summary>
        /// <returns>The dependant plugins.</returns>
        [NotNull, ItemNotNull]
        public IEnumerable<PluginDependencyTreeNode> GetAllDependants()
        {
            foreach (var dependant in this.Dependants)
            {
                yield return dependant;

                foreach (var sub in dependant.GetAllDependants())
                {
                    yield return sub;
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{this.Plugin} => ({string.Join(", ", _dependants.Select(d => d.Plugin))})";
        }
    }
}
