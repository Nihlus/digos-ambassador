//
//  ModuleDocumentationGenerator.cs
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
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Doc.Data;
using DIGOS.Ambassador.Doc.Extensions;
using DIGOS.Ambassador.Doc.Reflection;
using Humanizer;
using JetBrains.Annotations;
using Mono.Cecil;
using Mono.Cecil.Rocks;
using Remora.Markdown;
using static Remora.Markdown.EmphasisType;

namespace DIGOS.Ambassador.Doc
{
    /// <summary>
    /// Generates Markdown documentation for Discord.Net command modules.
    /// </summary>
    public class ModuleDocumentationGenerator : IDocumentationGenerator
    {
        private readonly PlaceholderData _placeholderData;
        private readonly IEnumerable<ModuleDefinition> _commandAssemblyModules;
        private readonly string _outputPath;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleDocumentationGenerator"/> class.
        /// </summary>
        /// <param name="commandAssemblyModules">The assembly containing the commands.</param>
        /// <param name="outputPath">The output path where documentation files should be written.</param>
        /// <param name="placeholderData">The placeholder repository.</param>
        public ModuleDocumentationGenerator
        (
            [NotNull] IEnumerable<ModuleDefinition> commandAssemblyModules,
            [NotNull] string outputPath,
            [NotNull] PlaceholderData placeholderData
        )
        {
            _commandAssemblyModules = commandAssemblyModules;
            _outputPath = outputPath;
            _placeholderData = placeholderData;
        }

        /// <inheritdoc />
        public void GenerateDocumentation()
        {
            GenerateDocumentationAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task GenerateDocumentationAsync()
        {
            var types = _commandAssemblyModules.SelectMany(c => c.Types)
                .Where(t => t.IsProbablyCommandModule());

            var modules = types.Select(t =>
            {
                var wasCreated = ModuleInformation.TryCreate(t, out var info);
                return (wasCreated, info);
            })
            .Where(wi => wi.wasCreated)
            .Select(wi => wi.info!)
            .OrderBy(i => i.Name)
            .ToList();

            var modulePages = GenerateDocumentationPages(modules);

            await Task.WhenAll(modulePages.Values.Select(p => SavePageAsync(p, Path.Combine("docs", "modules"))));

            var indexPage = GenerateDocumentationIndex
            (
                modulePages.Where(p => !p.Key.IsSubmodule).Select(kvp => kvp.Value)
            );

            await SavePageAsync(indexPage, "docs");
        }

        /// <summary>
        /// Generates documentation pages for the given modules, and their submodules.
        /// </summary>
        /// <param name="modules">The modules to generate documentation pages for.</param>
        /// <returns>A set of paired <see cref="ModuleInformation"/> and <see cref="MarkdownPage"/> objects.</returns>
        [NotNull]
        protected virtual Dictionary<ModuleInformation, MarkdownPage> GenerateDocumentationPages
        (
            [NotNull, ItemNotNull] IEnumerable<ModuleInformation> modules
        )
        {
            var modulePages = new Dictionary<ModuleInformation, MarkdownPage>();
            foreach (var module in modules)
            {
                modulePages.Add(module, GenerateModuleDocumentation(module));

                if (!module.Submodules.Any())
                {
                    continue;
                }

                var submodules = new List<ModuleInformation>(module.Submodules);
                while (submodules.Any())
                {
                    var completedModules = new List<ModuleInformation>();
                    var newSubmodules = new List<ModuleInformation>();
                    foreach (var submodule in submodules)
                    {
                        modulePages.Add(submodule, GenerateModuleDocumentation(submodule));

                        completedModules.Add(submodule);

                        if (submodule.Submodules.Any())
                        {
                            newSubmodules.AddRange(submodule.Submodules);
                        }
                    }

                    foreach (var completedModule in completedModules)
                    {
                        submodules.Remove(completedModule);
                    }

                    completedModules.Clear();
                    submodules.AddRange(newSubmodules);
                    newSubmodules.Clear();
                }
            }

            return modulePages;
        }

        /// <summary>
        /// Generates a table of contents-style index page for the documented modules.
        /// </summary>
        /// <param name="modulePages">The pages to include in the index.</param>
        /// <returns>A page with the index.</returns>
        [NotNull]
        protected virtual MarkdownPage GenerateDocumentationIndex([NotNull, ItemNotNull] IEnumerable<MarkdownPage> modulePages)
        {
            var moduleList = new MarkdownList();
            foreach (var modulePage in modulePages)
            {
                moduleList.AppendItem(new MarkdownLink($"modules/{modulePage.Name}.md", modulePage.Title.Humanize().Transform(To.TitleCase)));
            }

            var page = new MarkdownPage("index", "Index")
            .AppendSection
            (
                new MarkdownSection("Available Command Modules", 3)
                .AppendContent
                (
                    moduleList
                )
            );

            page.Footer = "<sub><sup>Generated by DIGOS.Ambassador.Doc</sup></sub>";

            return page;
        }

        /// <summary>
        /// Saves the given page to disk in the folder specified by <see cref="_outputPath"/>.
        /// </summary>
        /// <param name="page">The page to save.</param>
        /// <param name="subdirectory">The subdirectory to save it in, if any.</param>
        /// <returns>A task that must be awaited.</returns>
        private async Task SavePageAsync([NotNull] MarkdownPage page, string? subdirectory = null)
        {
            subdirectory ??= string.Empty;

            var outputDirectory = Path.Combine(_outputPath, subdirectory);
            Directory.CreateDirectory(outputDirectory);

            var outputPath = Path.Combine(outputDirectory, $"{page.Name}.md");

            await File.WriteAllTextAsync(outputPath, page.Compile(), Encoding.UTF8);
        }

        /// <summary>
        /// Generates a Markdown page with documentation for the given module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>A Markdown page.</returns>
        [NotNull]
        protected virtual MarkdownPage GenerateModuleDocumentation([NotNull] ModuleInformation module)
        {
            var page = new MarkdownPage
            (
                module.GetNameChain().Replace(" ", "_"),
                $"{module.GetNameChain().Humanize()} commands".Transform(To.TitleCase)
            );

            page.Footer = "<sub><sup>Generated by DIGOS.Ambassador.Doc</sup></sub>";

            page.AppendSection(GenerateSummarySection(module));

            if (module.Submodules.Any())
            {
                page.AppendSection(GenerateSubmodulesSection(module));
            }

            if (module.Commands.Any())
            {
                page.AppendSection(GenerateCommandsSection(module));
            }

            return page;
        }

        /// <summary>
        /// Generates a Markdown section with summary information about a module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>A Markdown section with the information.</returns>
        [NotNull]
        protected virtual MarkdownSection GenerateSummarySection([NotNull] ModuleInformation module)
        {
            string modulePrefixText;
            if (!module.HasPrefix)
            {
                modulePrefixText = "These commands have no prefix.";
            }
            else
            {
                var moduleAliasChains = module.GetAliasChains().ToList();

                var compiledAliases = moduleAliasChains
                    .Skip(1)
                    .Select(a => new MarkdownInlineCode(a).Compile())
                    .Humanize("or");

                var moduleExtraAliases = moduleAliasChains.Skip(1).Any()
                    ? $"You can also use {compiledAliases} instead of `{module.GetNameChain(true)}`."
                    : string.Empty;

                modulePrefixText = $"These commands are prefixed with `{module.GetNameChain(true)}`. {moduleExtraAliases}";
            }

            var summarySection = new MarkdownSection("Summary", 2).AppendContent
            (
                new MarkdownParagraph()
                    .AppendLine(modulePrefixText)
                    .AppendLine(module.Summary)
            );

            return summarySection;
        }

        /// <summary>
        /// Generates a Markdown section with information about the commands in a module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>A Markdown section with the information.</returns>
        [NotNull]
        protected virtual MarkdownSection GenerateCommandsSection([NotNull] ModuleInformation module)
        {
            var moduleCommandsSection = new MarkdownSection("Commands", 2);
            var commandGroups = module.Commands.GroupBy(c => c.Name).ToList();

            foreach (var commandGroup in commandGroups)
            {
                if (commandGroup != commandGroups.First())
                {
                    moduleCommandsSection.AppendContent(new MarkdownHorizontalRule());
                }

                var commandOverloads = new MarkdownSection("Overloads", 4);
                foreach (var command in commandGroup)
                {
                    commandOverloads.AppendContentRange(GenerateCommandOverloadContent(command));
                }

                // Filter out commands without names (we use the module's name chain instead)
                var commandGroupName = commandGroup.Key.IsNullOrWhitespace()
                    ? commandGroup.First().Module.GetNameChain(true)
                    : commandGroup.Key;

                var commandSection = new MarkdownSection(commandGroupName, 3).AppendContent(commandOverloads);
                commandSection.Header.Title.Emphasis = Italic;

                moduleCommandsSection.AppendContent(commandSection);
            }

            return moduleCommandsSection;
        }

        /// <summary>
        /// Generates an example of command usage, based on the signature of the command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>The example usage.</returns>
        [NotNull]
        protected virtual string GenerateCommandExample([NotNull] CommandInformation command)
        {
            var exampleBuilder = new StringBuilder();
            exampleBuilder.Append('!');
            exampleBuilder.Append(GetInvokableCommands(command).First());

            foreach (var parameter in command.Parameters)
            {
                if (exampleBuilder[^1] != ' ')
                {
                    exampleBuilder.Append(" ");
                }

                var typeDefinition = parameter.ParameterType.Resolve();
                if (parameter.ParameterType.Name.StartsWith("Nullable") && parameter.ParameterType.IsGenericInstance)
                {
                    var genericInstance = (GenericInstanceType)parameter.ParameterType;
                    typeDefinition = genericInstance.GenericArguments.First().Resolve();
                }

                if (typeDefinition.IsEnum)
                {
                    var firstOption = typeDefinition.Fields.First
                    (
                        f => !(f.Name.EndsWith("_") || f.Name.StartsWith('_'))
                    );

                    var humanizedOption = firstOption.Name.Humanize();
                    exampleBuilder.Append
                    (
                        humanizedOption.Contains(' ')
                            ? $"\"{humanizedOption}\""
                            : $"{humanizedOption}"
                    );

                    continue;
                }

                if (_placeholderData.HasPlaceholders(typeDefinition))
                {
                    if (parameter.ParameterType.IsArray)
                    {
                        // Get two examples
                        var placeholders = _placeholderData.GetPlaceholders(typeDefinition, 2);

                        foreach (var placeholder in placeholders)
                        {
                            exampleBuilder.Append(placeholder.Contains(' ') ? placeholder.Quote() : placeholder);
                            exampleBuilder.Append(' ');
                        }
                    }
                    else
                    {
                        var placeholder = _placeholderData.GetPlaceholders(typeDefinition).First();
                        if (placeholder.Contains(' '))
                        {
                            placeholder = placeholder.Quote();
                        }

                        exampleBuilder.Append(placeholder);
                    }

                    continue;
                }

                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"No placeholder data available for type {typeDefinition.Name}.");

                exampleBuilder.Append("\"placeholder\"");
            }

            return exampleBuilder.ToString().Trim();
        }

        /// <summary>
        /// Generates the Markdown content of a command overload.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A list of Markdown content nodes.</returns>
        [NotNull, ItemNotNull]
        protected virtual IEnumerable<IMarkdownNode> GenerateCommandOverloadContent([NotNull] CommandInformation command)
        {
            var invokableCommands = GetInvokableCommands(command).ToList();
            var prefix = invokableCommands.Count > 2
                ? "as well as"
                : "or";

            var commandExtraAliases = invokableCommands.Skip(1).Any()
                ? $"({prefix} {invokableCommands.Skip(1).Select(a => new MarkdownInlineCode(a).Compile()).Humanize("or")})"
                : string.Empty;

            var commandDisplayAliases =
                $"{new MarkdownInlineCode(GenerateCommandExample(command)).Compile()} {commandExtraAliases}".Trim();

            yield return new MarkdownParagraph()
            .AppendLine
            (
                new MarkdownText(commandDisplayAliases)
                {
                    Emphasis = Bold
                }
            )
            .AppendText(command.Summary);

            if (command.Parameters.Count > 0)
            {
                yield return GenerateCommandParameterTable(command);
            }
        }

        /// <summary>
        /// Generates a Markdown table with information about the parameters of the given command.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A Markdown table.</returns>
        [NotNull]
        protected virtual MarkdownTable GenerateCommandParameterTable([NotNull] CommandInformation command)
        {
            var parameterTable = new MarkdownTable();
            parameterTable.AppendColumn(new MarkdownTableColumn("Name"));
            parameterTable.AppendColumn(new MarkdownTableColumn("Type"));
            parameterTable.AppendColumn(new MarkdownTableColumn("Optional"));

            foreach (var parameter in command.Parameters)
            {
                var typeDefinition = parameter.ParameterType;
                if (typeDefinition.Name.StartsWith("Nullable") && typeDefinition.IsGenericInstance)
                {
                    var genericInstance = (GenericInstanceType)parameter.ParameterType;
                    typeDefinition = genericInstance.GenericArguments.First().Resolve();
                }

                var row = new MarkdownTableRow()
                    .AppendCell(new MarkdownText(parameter.Name))
                    .AppendCell(new MarkdownText(typeDefinition.Humanize()))
                    .AppendCell(new MarkdownInlineCode(parameter.IsOptional ? "yes" : "no"));

                parameterTable.AppendRow(row);
            }

            return parameterTable;
        }

        /// <summary>
        /// Generates a Markdown section listing the submodules in the given module.
        /// </summary>
        /// <param name="module">The module.</param>
        /// <returns>A Markdown section with the submodules.</returns>
        [NotNull]
        protected virtual MarkdownSection GenerateSubmodulesSection([NotNull] ModuleInformation module)
        {
            var submoduleSection = new MarkdownSection("Submodules", 2);
            var submoduleList = new MarkdownList
            {
                Type = ListType.Bullet
            };

            submoduleSection.AppendContent(submoduleList);

            foreach (var submodule in module.Submodules)
            {
                submoduleList.AppendItem
                (
                    new MarkdownLink
                    (
                        $"{submodule.GetNameChain().Replace(" ", "_")}.md",
                        submodule.Name.Humanize()
                    )
                );
            }

            return submoduleSection;
        }

        /// <summary>
        /// Generates the actual invokable commands for a given command.
        /// </summary>
        /// <param name="information">The command.</param>
        /// <returns>The invokable commands.</returns>
        private IEnumerable<string> GetInvokableCommands(CommandInformation information)
        {
            var moduleNameChain = information.Module.GetNameChain(true);

            foreach (var alias in information.Aliases)
            {
                yield return $"{moduleNameChain} {alias}".Trim();
            }
        }
    }
}
