//
//  PluginDependencyTree.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins
{
    /// <summary>
    /// Represents a tree of plugins, ordered by their dependencies.
    /// </summary>
    [PublicAPI]
    public class PluginDependencyTree
    {
        private readonly List<PluginDependencyTreeNode> _branches;

        /// <summary>
        /// Gets the root nodes of the identified plugin dependency branches. The root node is considered to be the
        /// application itself, which is implicitly initialized.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<PluginDependencyTreeNode> Branches => _branches;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginDependencyTree"/> class.
        /// </summary>
        /// <param name="branches">The dependency branches.</param>
        public PluginDependencyTree([CanBeNull, ItemNotNull] List<PluginDependencyTreeNode> branches = null)
        {
            _branches = branches ?? new List<PluginDependencyTreeNode>();
        }

        /// <summary>
        /// Adds a dependency branch to the tree.
        /// </summary>
        /// <param name="branch">The branch.</param>
        public void AddBranch(PluginDependencyTreeNode branch)
        {
            if (_branches.Contains(branch))
            {
                return;
            }

            _branches.Add(branch);
        }
    }
}
