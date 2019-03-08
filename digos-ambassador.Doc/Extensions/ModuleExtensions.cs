//
//  ModuleExtensions.cs
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

using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Doc.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="ModuleInfo"/> class.
    /// </summary>
    public static class ModuleExtensions
    {
        /// <summary>
        /// Gets the name chain of a module, that is, the name of its parent followed by the module's name.
        /// </summary>
        /// <param name="this">The module to get the chain of.</param>
        /// <returns>A name chain in the form of "[parentName] [childName]".</returns>
        [NotNull]
        public static string GetNameChain([NotNull] this ModuleInfo @this)
        {
            return @this.IsSubmodule ? $"{GetNameChain(@this.Parent)} {@this.Name}" : @this.Name;
        }
    }
}
