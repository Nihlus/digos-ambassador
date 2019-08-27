//
//  ModuleInformation.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DIGOS.Ambassador.Doc.Extensions;
using JetBrains.Annotations;
using Mono.Cecil;

namespace DIGOS.Ambassador.Doc.Reflection
{
    /// <summary>
    /// Represents information about a Discord command module.
    /// </summary>
    [PublicAPI]
    public class ModuleInformation
    {
        /// <summary>
        /// Gets the name of the module.
        /// </summary>
        [NotNull]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the summary of the module.
        /// </summary>
        [NotNull]
        public string Summary { get; private set; }

        /// <summary>
        /// Gets the aliases of the module, if any.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<string> Aliases { get; private set; }

        /// <summary>
        /// Gets the commands defined in the module.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<CommandInformation> Commands { get; private set; }

        /// <summary>
        /// Gets the submodules defined in the module.
        /// </summary>
        [NotNull, ItemNotNull]
        public IReadOnlyCollection<ModuleInformation> Submodules { get; private set; }

        /// <summary>
        /// Gets the parent module, if any.
        /// </summary>
        [CanBeNull]
        public ModuleInformation Parent { get; private set; }

        /// <summary>
        /// Gets a value indicating whether this module is a submodule.
        /// </summary>
        public bool IsSubmodule => !(this.Parent is null);

        /// <summary>
        /// Gets a value indicating whether this module has a prefix.
        /// </summary>
        public bool HasPrefix { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleInformation"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Only used in TryCreate")]
        private ModuleInformation()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleInformation"/> class.
        /// </summary>
        /// <param name="moduleType">The type definition of the module.</param>
        /// <param name="information">The created information.</param>
        /// <param name="parentModule">The parent module.</param>
        /// <returns>true if the information was successfully created; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> true, information : notnull; => false, information : null")]
        public static bool TryCreate
        (
            [NotNull] TypeDefinition moduleType,
            [CanBeNull] out ModuleInformation information,
            [CanBeNull] ModuleInformation parentModule = null
        )
        {
            information = null;

            if (!moduleType.TryGetModuleName(out var name))
            {
                return false;
            }

            bool hasPrefix = moduleType.TryGetGroup(out _);

            if (!moduleType.TryGetSummary(out var summary))
            {
                summary = string.Empty;
            }

            if (!moduleType.TryGetAliases(out var aliases))
            {
                aliases = new string[] { };
            }

            var allAliases = new List<string> { name };
            allAliases.AddRange(aliases);

            allAliases = allAliases.Distinct().ToList();

            var newInformation = new ModuleInformation
            {
                Name = name,
                Summary = summary,
                Aliases = allAliases,
                Parent = parentModule,
                HasPrefix = hasPrefix
            };

            if (!moduleType.TryGetCommands(newInformation, out var commands))
            {
                commands = new CommandInformation[] { };
            }

            newInformation.Commands = commands;

            if (!moduleType.TryGetSubmodules(newInformation, out var submodules))
            {
                submodules = new ModuleInformation[] { };
            }

            newInformation.Submodules = submodules;

            information = newInformation;

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Name;
        }
    }
}
