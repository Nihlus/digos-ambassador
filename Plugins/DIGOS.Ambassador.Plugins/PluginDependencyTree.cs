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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Results.Base;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins
{
    /// <summary>
    /// Represents a tree of plugins, ordered by their dependencies.
    /// </summary>
    [PublicAPI]
    public class PluginDependencyTree
    {
        [NotNull, ItemNotNull]
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
        /// Walks the plugin tree, performing the given operation on each node. If the operation fails, the walk
        /// terminates at that point.
        /// </summary>
        /// <param name="errorFactory">A factory method to create an error result.</param>
        /// <param name="preOperation">The operation to perform while walking down into the tree.</param>
        /// <param name="postOperation">The operation to perform while walking up into the tree.</param>
        /// <typeparam name="TResult">The result type.</typeparam>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        [NotNull, ItemNotNull]
        public async Task<IEnumerable<TResult>> WalkAsync<TResult>
        (
            [NotNull] Func<PluginDependencyTreeNode, Exception, TResult> errorFactory,
            [NotNull] Func<PluginDependencyTreeNode, Task<TResult>> preOperation,
            [CanBeNull] Func<PluginDependencyTreeNode, Task<TResult>> postOperation = null
        )
            where TResult : IResult
        {
            var results = new List<TResult>();
            foreach (var branch in _branches)
            {
                results.AddRange(await WalkNodeAsync(branch, errorFactory, preOperation, postOperation));
            }

            return results;
        }

        [NotNull, ItemNotNull]
        private async Task<IEnumerable<TResult>> WalkNodeAsync<TResult>
        (
            [NotNull] PluginDependencyTreeNode node,
            [NotNull] Func<PluginDependencyTreeNode, Exception, TResult> errorFactory,
            [NotNull] Func<PluginDependencyTreeNode, Task<TResult>> preOperation,
            [CanBeNull] Func<PluginDependencyTreeNode, Task<TResult>> postOperation = null
        )
            where TResult : IResult
        {
            var results = new List<TResult>();

            try
            {
                var result = await preOperation(node);
                results.Add(result);

                if (!result.IsSuccess)
                {
                    results.AddRange(node.GetAllDependants().Select(d => errorFactory(d, null)));
                    return results;
                }
            }
            catch (Exception e)
            {
                results.Add(errorFactory(node, e));
                results.AddRange(node.GetAllDependants().Select(d => errorFactory(d, null)));
                return results;
            }

            foreach (var dependant in node.Dependants)
            {
                results.AddRange(await WalkNodeAsync(dependant, errorFactory, preOperation, postOperation));
            }

            if (!(postOperation is null))
            {
                try
                {
                    var result = await postOperation(node);
                    results.Add(result);

                    if (!result.IsSuccess)
                    {
                        results.AddRange(node.GetAllDependants().Select(d => errorFactory(d, null)));
                        return results;
                    }
                }
                catch (Exception e)
                {
                    results.Add(errorFactory(node, e));
                    results.AddRange(node.GetAllDependants().Select(d => errorFactory(d, null)));
                    return results;
                }
            }

            return results;
        }

        /// <summary>
        /// Adds a dependency branch to the tree.
        /// </summary>
        /// <param name="branch">The branch.</param>
        internal void AddBranch([NotNull] PluginDependencyTreeNode branch)
        {
            if (_branches.Contains(branch))
            {
                return;
            }

            _branches.Add(branch);
        }
    }
}
