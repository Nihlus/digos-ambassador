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
using System.Reflection;
using DIGOS.Ambassador.Plugins.Abstractions;
using DIGOS.Ambassador.Plugins.Abstractions.Attributes;

namespace DIGOS.Ambassador.Plugins.Services
{
    /// <summary>
    /// Serves functionality related to plugins.
    /// </summary>
    public class PluginService
    {
        /// <summary>
        /// Loads the available plugins.
        /// </summary>
        /// <returns>The descriptors of the available plugins.</returns>
        public IEnumerable<IPluginDescriptor> LoadAvailablePlugins()
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
                    assembly = Assembly.LoadFile(assemblyPath);
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

                var descriptor = (IPluginDescriptor)Activator.CreateInstance(pluginAttribute.PluginDescriptor);
                yield return descriptor;
            }
        }
    }
}
