//
//  TypeInspectionExtensions.cs
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

using System.Linq;
using DIGOS.Ambassador.Doc.Reflection;
using JetBrains.Annotations;
using Mono.Cecil;

namespace DIGOS.Ambassador.Doc.Extensions
{
    /// <summary>
    /// Contains extension methods for the <see cref="TypeDefinition"/> class, which provide ways to inspect various
    /// Discord-related attributes.
    /// </summary>
    [PublicAPI]
    public static class TypeInspectionExtensions
    {
        /// <summary>
        /// Attempts to retrieve the module's name.
        /// </summary>
        /// <param name="provider">The attribute provider.</param>
        /// <param name="name">The name.</param>
        /// <returns>true if the name was successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, name : null; => true, name : notnull")]
        public static bool TryGetModuleName
        (
            [NotNull] this TypeDefinition provider,
            [CanBeNull] out string name
        )
        {
            name = null;

            if (provider.TryGetGroup(out name))
            {
                return true;
            }

            if (provider.TryGetName(out name))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Attempts to retrieve the definition's group.
        /// </summary>
        /// <param name="provider">The attribute provider.</param>
        /// <param name="group">The group.</param>
        /// <returns>true if the group was successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, group : null; => true, group : notnull")]
        public static bool TryGetGroup(this TypeDefinition provider, out string group)
        {
            group = null;

            var groupAttribute = provider.CustomAttributes.FirstOrDefault
            (
                a => a.AttributeType.Name == "GroupAttribute"
            );

            if (groupAttribute is null)
            {
                return false;
            }

            group = groupAttribute.ConstructorArguments.First().Value as string;

            return !(group is null);
        }

        /// <summary>
        /// Attempts to retrieve the provider's name.
        /// </summary>
        /// <param name="provider">The attribute provider.</param>
        /// <param name="name">The name.</param>
        /// <returns>true if the name was successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, name : null; => true, name : notnull")]
        public static bool TryGetName(this ICustomAttributeProvider provider, out string name)
        {
            name = null;

            var nameAttribute = provider.CustomAttributes.FirstOrDefault
            (
                a => a.AttributeType.Name == "NameAttribute"
            );

            if (nameAttribute is null)
            {
                return false;
            }

            name = nameAttribute.ConstructorArguments.First().Value as string;

            return !(name is null);
        }

        /// <summary>
        /// Attempts to retrieve the name of the command.
        /// </summary>
        /// <param name="provider">The attribute provider.</param>
        /// <param name="name">The name.</param>
        /// <returns>true if the name was successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, name : null; => true, name : notnull")]
        public static bool TryGetCommandName(this MethodDefinition provider, out string name)
        {
            name = null;

            var commandAttribute = provider.CustomAttributes.FirstOrDefault
            (
                a => a.AttributeType.Name == "CommandAttribute"
            );

            if (commandAttribute is null)
            {
                return false;
            }

            name = commandAttribute.ConstructorArguments.FirstOrDefault().Value as string;
            if (name is null)
            {
                // No command name passed means no name, and it's parsed as the module name.
                name = string.Empty;
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the summary attached to the definition.
        /// </summary>
        /// <param name="provider">The attribute provider.</param>
        /// <param name="summary">The summary.</param>
        /// <returns>true if the summary was successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, summary : null; => true, summary : notnull")]
        public static bool TryGetSummary
        (
            [NotNull] this ICustomAttributeProvider provider,
            [CanBeNull] out string summary
        )
        {
            summary = null;

            var summaryAttribute = provider.CustomAttributes.FirstOrDefault
            (
                a => a.AttributeType.Name == "SummaryAttribute"
            );

            if (summaryAttribute is null)
            {
                return false;
            }

            summary = summaryAttribute.ConstructorArguments.First().Value as string;

            if (summary is null)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the aliases attached to the definition.
        /// </summary>
        /// <param name="provider">The attribute provider.</param>
        /// <param name="aliases">The aliases.</param>
        /// <returns>true if the aliases were successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, aliases : null; => true, aliases : notnull")]
        public static bool TryGetAliases
        (
            [NotNull] this ICustomAttributeProvider provider,
            [CanBeNull] out string[] aliases
        )
        {
            aliases = null;

            var aliasesAttribute = provider.CustomAttributes.FirstOrDefault
            (
                a => a.AttributeType.Name == "AliasAttribute"
            );

            if (aliasesAttribute is null)
            {
                return false;
            }

            var aliasArguments = aliasesAttribute.ConstructorArguments.First().Value as CustomAttributeArgument[];
            if (aliasArguments is null)
            {
                return false;
            }

            aliases = aliasArguments.Select(a => a.Value as string).ToArray();

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the commands defined in the module.
        /// </summary>
        /// <param name="module">The attribute provider.</param>
        /// <param name="parentModule">The module that's being loaded.</param>
        /// <param name="commands">The commands.</param>
        /// <returns>true if the commands were successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, commands : null; => true, commands : notnull")]
        public static bool TryGetCommands
        (
            [NotNull] this TypeDefinition module,
            ModuleInformation parentModule,
            [CanBeNull] out CommandInformation[] commands
        )
        {
            commands = null;

            if (!module.HasMethods)
            {
                return false;
            }

            commands = module.Methods.Select
                (
                    m =>
                    {
                        var wasParsed = CommandInformation.TryCreate(m, parentModule, out var command);
                        return (wasParsed, command);
                    }
                )
                .Where(cr => cr.wasParsed)
                .Select(cr => cr.command)
                .ToArray();

            return true;
        }

        /// <summary>
        /// Attempts to retrieve the submodules defined in the module.
        /// </summary>
        /// <param name="module">The attribute provider.</param>
        /// <param name="parentModule">The module that's being loaded.</param>
        /// <param name="submodules">The submodules.</param>
        /// <returns>true if the submodules were successfully retrieved; otherwise, false.</returns>
        [Pure, ContractAnnotation("=> false, submodules : null; => true, submodules : notnull")]
        public static bool TryGetSubmodules
        (
            [NotNull] this TypeDefinition module,
            ModuleInformation parentModule,
            [CanBeNull] out ModuleInformation[] submodules
        )
        {
            submodules = null;

            if (!module.HasNestedTypes)
            {
                return false;
            }

            submodules = module.NestedTypes.Where(t => t.IsProbablyCommandModule()).Select
                (
                    t =>
                    {
                        var wasParsed = ModuleInformation.TryCreate(t, out var submodule, parentModule);
                        return (wasParsed, submodule);
                    }
                )
                .Where(mr => mr.wasParsed)
                .Select(mr => mr.submodule)
                .ToArray();

            return true;
        }

        /// <summary>
        /// Determines whether the given type is probably a command module.
        /// </summary>
        /// <param name="module">The potential module.</param>
        /// <returns>true if it's probably a command module; otherwise, false.</returns>
        public static bool IsProbablyCommandModule(this TypeDefinition module)
        {
            if (!module.IsClass)
            {
                return false;
            }

            if (module.BaseType is null)
            {
                return false;
            }

            if (module.IsAbstract)
            {
                return false;
            }

            return module.BaseType.FullName.Contains("ModuleBase");
        }
    }
}
