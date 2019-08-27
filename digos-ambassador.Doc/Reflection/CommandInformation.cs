//
//  CommandInformation.cs
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
    public class CommandInformation
    {
        /// <summary>
        /// Gets the name of the command.
        /// </summary>
        [NotNull]
        public string Name { get; private set; }

        /// <summary>
        /// Gets the summary of the command.
        /// </summary>
        [NotNull]
        public string Summary { get; private set; }

        /// <summary>
        /// Gets the aliases of the command, if any.
        /// </summary>
        [NotNull]
        public IReadOnlyCollection<string> Aliases { get; private set; }

        /// <summary>
        /// Gets the parameters of the command, if any.
        /// </summary>
        [NotNull]
        public IReadOnlyCollection<ParameterDefinition> Parameters { get; private set; }

        /// <summary>
        /// Gets the module that the command is defined in.
        /// </summary>
        [NotNull]
        public ModuleInformation Module { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandInformation"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Only used in TryCreate")]
        private CommandInformation()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CommandInformation"/> class.
        /// </summary>
        /// <param name="commandMethod">The type definition of the command method.</param>
        /// <param name="module">The module the command is defined in.</param>
        /// <param name="information">The created information.</param>
        /// <returns>true if the information was successfully created; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> true, information : notnull; => false, information : null")]
        public static bool TryCreate
        (
            [NotNull] MethodDefinition commandMethod,
            [NotNull] ModuleInformation module,
            [CanBeNull] out CommandInformation information
        )
        {
            information = null;

            if (!commandMethod.TryGetCommandName(out var name))
            {
                return false;
            }

            if (!commandMethod.TryGetSummary(out var summary))
            {
                summary = string.Empty;
            }

            if (!commandMethod.TryGetAliases(out var aliases))
            {
                aliases = new string[] { };
            }

            var allAliases = new List<string> { name };
            allAliases.AddRange(aliases);

            allAliases = allAliases.Distinct().ToList();

            var parameters = commandMethod.HasParameters
                ? commandMethod.Parameters.ToArray()
                : new ParameterDefinition[] { };

            information = new CommandInformation
            {
                Name = name,
                Summary = summary,
                Aliases = allAliases,
                Parameters = parameters,
                Module = module
            };

            return true;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return this.Name;
        }
    }
}
