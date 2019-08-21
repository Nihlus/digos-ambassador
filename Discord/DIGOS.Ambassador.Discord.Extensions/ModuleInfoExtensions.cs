//
//  ModuleInfoExtensions.cs
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
using System.Text;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Discord.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="ModuleInfo"/> class.
    /// </summary>
    public static class ModuleInfoExtensions
    {
        /// <summary>
        /// Gets the top-level module that contains the given module.
        /// </summary>
        /// <param name="this">The module.</param>
        /// <returns>The top-level module.</returns>
        [NotNull]
        public static ModuleInfo GetTopLevelModule([NotNull] this ModuleInfo @this)
        {
            var currentModule = @this;
            while (currentModule.IsSubmodule)
            {
                currentModule = currentModule.Parent;
            }

            return currentModule;
        }

        /// <summary>
        /// Gets all commands, including those from nested modules, from the given module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>The commands.</returns>
        public static IEnumerable<CommandInfo> GetAllCommands([NotNull] this ModuleInfo module)
        {
            var currentCommands = new List<CommandInfo>();

            currentCommands.AddRange(module.Commands);

            foreach (var submodule in module.Submodules)
            {
                currentCommands.AddRange(submodule.GetAllCommands());
            }

            return currentCommands;
        }

        /// <summary>
        /// Gets the full prefix of the module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>The full prefix.</returns>
        public static string GetFullPrefix([NotNull] this ModuleInfo module)
        {
            var sb = new StringBuilder();

            var current = module;
            while (!(current is null))
            {
                sb.Insert(0, $"{current.Name} ");

                current = current.Parent;
            }

            return sb.ToString();
        }
    }
}
