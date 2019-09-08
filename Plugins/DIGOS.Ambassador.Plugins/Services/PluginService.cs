//
//  PluginService.cs
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
using System.IO;
using System.Linq;
using System.Reflection;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Abstractions;
using DIGOS.Ambassador.Plugins.Abstractions.Attributes;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Services
{
    /// <summary>
    /// Serves functionality related to plugins.
    /// </summary>
    [PublicAPI]
    public sealed class PluginService
    {
        /// <summary>
        /// Loads the available plugins into a dependency tree.
        /// </summary>
        /// <returns>The dependency tree.</returns>
        [NotNull, Pure]
        public PluginDependencyTree LoadPluginDescriptors()
        {
            var pluginAssemblies = LoadAvailablePluginAssemblies().ToList();
            var pluginsWithDependencies = pluginAssemblies.ToDictionary
            (
                a => a,
                a => a.GetReferencedAssemblies()
                    .Where(ra => pluginAssemblies.Any(pa => pa.FullName == ra.FullName))
                    .Select(ra => pluginAssemblies.First(pa => pa.FullName == ra.FullName))
            );

            bool IsDependency(Assembly assembly, Assembly other)
            {
                var dependencies = pluginsWithDependencies[assembly];
                foreach (var dependency in dependencies)
                {
                    if (dependency == other)
                    {
                        return true;
                    }

                    if (IsDependency(dependency, other))
                    {
                        return true;
                    }
                }

                return false;
            }

            bool IsDirectDependency(Assembly assembly, Assembly dependency)
            {
                var dependencies = pluginsWithDependencies[assembly];
                if (IsDependency(assembly, dependency) && dependencies.All(d => !IsDependency(d, dependency)))
                {
                    return true;
                }

                return false;
            }

            var tree = new PluginDependencyTree();
            var nodes = new Dictionary<Assembly, PluginDependencyTreeNode>();

            var sorted = pluginsWithDependencies.Keys.TopologicalSort(k => pluginsWithDependencies[k]).ToList();
            while (sorted.Count > 0)
            {
                var current = sorted[0];
                var node = new PluginDependencyTreeNode(LoadPluginDescriptor(current));

                var dependencies = pluginsWithDependencies[current].ToList();
                if (!dependencies.Any())
                {
                    // This is a root of a chain
                    tree.AddBranch(node);
                }

                foreach (var dependency in dependencies)
                {
                    if (!IsDirectDependency(current, dependency))
                    {
                        continue;
                    }

                    var dependencyNode = nodes[dependency];
                    dependencyNode.AddDependant(node);
                }

                nodes.Add(current, node);
                sorted.Remove(current);
            }

            return tree;
        }

        /// <summary>
        /// Loads the plugin descriptor from the given assembly.
        /// </summary>
        /// <param name="assembly">The assembly.</param>
        /// <returns>The plugin descriptor.</returns>
        [NotNull, Pure]
        public IPluginDescriptor LoadPluginDescriptor([NotNull] Assembly assembly)
        {
            var pluginAttribute = assembly.GetCustomAttribute<AmbassadorPlugin>();
            var descriptor = (IPluginDescriptor)Activator.CreateInstance(pluginAttribute.PluginDescriptor);
            return descriptor;
        }

        /// <summary>
        /// Loads the available plugin assemblies.
        /// </summary>
        /// <returns>The available assemblies.</returns>
        [Pure, NotNull, ItemNotNull]
        public IEnumerable<Assembly> LoadAvailablePluginAssemblies()
        {
            var entryAssemblyPath = Assembly.GetEntryAssembly()?.Location;

            if (entryAssemblyPath is null)
            {
                yield break;
            }

            var installationDirectory = Directory.GetParent(entryAssemblyPath);
            var assemblies = Directory.EnumerateFiles
            (
                installationDirectory.FullName,
                "*.dll",
                SearchOption.AllDirectories
            );

            foreach (var assemblyPath in assemblies)
            {
                Assembly assembly;
                try
                {
                    assembly = Assembly.LoadFrom(assemblyPath);
                }
                catch
                {
                    continue;
                }

                var pluginAttribute = assembly.GetCustomAttribute<AmbassadorPlugin>();
                if (pluginAttribute is null)
                {
                    continue;
                }

                yield return assembly;
            }
        }

        /// <summary>
        /// Loads the available plugins.
        /// </summary>
        /// <returns>The descriptors of the available plugins.</returns>
        [Pure, NotNull, ItemNotNull]
        public IEnumerable<IPluginDescriptor> LoadAvailablePlugins()
        {
            var pluginAssemblies = LoadAvailablePluginAssemblies().ToList();
            var sorted = pluginAssemblies.TopologicalSort
            (
                a => a.GetReferencedAssemblies()
                    .Where
                    (
                        n => pluginAssemblies.Any(pa => pa.GetName().FullName == n.FullName)
                    )
                    .Select
                    (
                        n => pluginAssemblies.First(pa => pa.GetName().FullName == n.FullName)
                    )
            );

            foreach (var pluginAssembly in sorted)
            {
                var pluginAttribute = pluginAssembly.GetCustomAttribute<AmbassadorPlugin>();
                var descriptor = (IPluginDescriptor)Activator.CreateInstance(pluginAttribute.PluginDescriptor);
                yield return descriptor;
            }
        }
    }
}
