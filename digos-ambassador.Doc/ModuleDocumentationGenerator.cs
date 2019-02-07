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
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using DIGOS.Ambassador.Doc.Extensions;
using DIGOS.Ambassador.Doc.Nodes;
using DIGOS.Ambassador.Extensions;

using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using static DIGOS.Ambassador.Doc.Nodes.EmphasisType;

namespace DIGOS.Ambassador.Doc
{
    /// <summary>
    /// Generates Markdown documentation for Discord.Net command modules.
    /// </summary>
    public class ModuleDocumentationGenerator : IDocumentationGenerator
    {
        private readonly Assembly CommandAssembly;
        private readonly string OutputPath;

        private readonly CommandService Commands;

        private readonly Regex TypeReaderTypeFinder = new Regex("(?<=No type reader found for type ).+?.(?=, one must be specified)", RegexOptions.Compiled);

        private IServiceProvider Services = new ServiceCollection().BuildServiceProvider();

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleDocumentationGenerator"/> class.
        /// </summary>
        /// <param name="commandAssembly">The assembly containing the commands.</param>
        /// <param name="outputPath">The output path where documentation files should be written.</param>
        public ModuleDocumentationGenerator(Assembly commandAssembly, string outputPath)
        {
            this.CommandAssembly = commandAssembly;
            this.OutputPath = outputPath;

            this.Commands = new CommandService(new CommandServiceConfig { ThrowOnError = false });
        }

        /// <summary>
        /// Adds a type reader to the internal command service.
        /// </summary>
        /// <param name="typeReader">The type reader.</param>
        /// <typeparam name="T">The type that the reader reads.</typeparam>
        /// <returns>The generator, with the type reader added.</returns>
        [NotNull]
        public ModuleDocumentationGenerator WithTypeReader<T>(TypeReader typeReader)
        {
            this.Commands.AddTypeReader<T>(typeReader);
            return this;
        }

        /// <inheritdoc />
        public void GenerateDocumentation()
        {
            GenerateDocumentationAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public async Task GenerateDocumentationAsync()
        {
            await AddModulesAsync();

            var modules = GetTopLevelModules(this.Commands.Modules);
            var modulePages = GenerateDocumentationPages(modules);

            foreach (var modulePage in modulePages.Values)
            {
                await SavePageAsync(modulePage, "modules");
            }

            var indexPage = GenerateDocumentationIndex
            (
                modulePages.Where(p => !p.Key.IsSubmodule).Select(kvp => kvp.Value)
            );

            await SavePageAsync(indexPage);
        }

        private async Task AddModulesAsync()
        {
            while (true)
            {
                try
                {
                    await this.Commands.AddModulesAsync(this.CommandAssembly, this.Services);
                    break;
                }
                catch (InvalidOperationException iox)
                {
                    var typeName = this.TypeReaderTypeFinder.Match(iox.Message).Value;
                    var typeInfo = this.CommandAssembly.DefinedTypes.FirstOrDefault(t => t.Name == typeName);
                    if (typeInfo is null)
                    {
                        throw;
                    }

                    this.Commands.AddTypeReader(typeInfo.AsType(), new DummyTypeReader());
                }
            }
        }

        /// <summary>
        /// Generates documentation pages for the given modules, and their submodules.
        /// </summary>
        /// <param name="modules">The modules to generate documentation pages for.</param>
        /// <returns>A set of paired <see cref="ModuleInfo"/> and <see cref="MarkdownPage"/> objects.</returns>
        [NotNull]
        protected virtual Dictionary<ModuleInfo, MarkdownPage> GenerateDocumentationPages([NotNull, ItemNotNull] IEnumerable<ModuleInfo> modules)
        {
            var modulePages = new Dictionary<ModuleInfo, MarkdownPage>();
            foreach (var module in modules)
            {
                modulePages.Add(module, GenerateModuleDocumentation(module));

                if (!module.Submodules.Any())
                {
                    continue;
                }

                var submodules = new List<ModuleInfo>(module.Submodules);
                while (submodules.Any())
                {
                    var completedModules = new List<ModuleInfo>();
                    var newSubmodules = new List<ModuleInfo>();
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
        /// Determines whether or not the given module has a prefix.
        /// </summary>
        /// <param name="info">The module.</param>
        /// <returns>true if the module has a prefix; otherwise, false.</returns>
        private bool HasPrefix([NotNull] ModuleInfo info)
        {
            // Workaround for empty ModuleInfo::Attributes
            var baseAlias = info.GetNameChain();
            return info.Aliases.Contains(baseAlias);
        }

        /// <summary>
        /// Saves the given page to disk in the folder specified by <see cref="OutputPath"/>.
        /// </summary>
        /// <param name="page">The page to save.</param>
        /// <param name="subdirectory">The subdirectory to save it in, if any.</param>
        /// <returns>A task that must be awaited.</returns>
        private async Task SavePageAsync([NotNull] MarkdownPage page, [CanBeNull] string subdirectory = null)
        {
            subdirectory = subdirectory ?? string.Empty;

            var outputDirectory = Path.Combine(this.OutputPath, subdirectory);
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
        protected virtual MarkdownPage GenerateModuleDocumentation([NotNull] ModuleInfo module)
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
        protected virtual MarkdownSection GenerateSummarySection([NotNull] ModuleInfo module)
        {
            string modulePrefixText;
            if (!HasPrefix(module))
            {
                modulePrefixText = "These commands have no prefix.";
            }
            else
            {
                var relevantModuleAliases = module.Aliases.Skip(1).Select(a => new MarkdownInlineCode(a).Compile());
                var moduleExtraAliases = module.Aliases.Count > 1
                    ? $"You can also use {relevantModuleAliases.Humanize("or")} instead of `{module.GetNameChain()}`."
                    : string.Empty;

                modulePrefixText = $"These commands are prefixed with `{module.GetNameChain()}`. {moduleExtraAliases}";
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
        protected virtual MarkdownSection GenerateCommandsSection([NotNull] ModuleInfo module)
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

                var commandSection = new MarkdownSection(commandGroup.Key, 3).AppendContent(commandOverloads);
                commandSection.Header.Title.Emphasis = Italic;

                moduleCommandsSection.AppendContent(commandSection);
            }

            return moduleCommandsSection;
        }

        /// <summary>
        /// Generates the Markdown content of a command overload.
        /// </summary>
        /// <param name="command">The command.</param>
        /// <returns>A list of Markdown content nodes.</returns>
        [NotNull, ItemNotNull]
        protected virtual IEnumerable<IMarkdownNode> GenerateCommandOverloadContent([NotNull] CommandInfo command)
        {
            var relevantAliases = command.Aliases.Skip(1).Where(a => a.StartsWith(command.Module.Aliases.First())).ToList();
            var prefix = relevantAliases.Count > 1
                ? "as well as"
                : "or";

            var commandExtraAliases = relevantAliases.Any()
                ? $"({prefix} {relevantAliases.Select(a => new MarkdownInlineCode(a).Compile()).Humanize("or")})"
                : string.Empty;

            var commandDisplayAliases = $"{new MarkdownInlineCode(command.Aliases.First()).Compile()} {commandExtraAliases}".Trim();

            yield return new MarkdownParagraph()
            .AppendLine
            (
                new MarkdownText
                {
                    Content = commandDisplayAliases,
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
        protected virtual MarkdownTable GenerateCommandParameterTable([NotNull] CommandInfo command)
        {
            var parameterTable = new MarkdownTable();
            parameterTable.AppendColumn(new MarkdownTableColumn("Name"));
            parameterTable.AppendColumn(new MarkdownTableColumn("Type"));
            parameterTable.AppendColumn(new MarkdownTableColumn("Optional"));

            foreach (var parameter in command.Parameters)
            {
                var row = new MarkdownTableRow()
                    .AppendCell(new MarkdownText(parameter.Name))
                    .AppendCell(new MarkdownText(parameter.Type.Humanize()))
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
        protected virtual MarkdownSection GenerateSubmodulesSection([NotNull] ModuleInfo module)
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
        /// Gets a list of the top-level modules in the given set of modules. This method recursively reduces or expands
        /// the set as needed.
        /// </summary>
        /// <param name="modules">The modules to scan.</param>
        /// <returns>A list of the top-level modules.</returns>
        [NotNull, ItemNotNull]
        private IEnumerable<ModuleInfo> GetTopLevelModules([NotNull, ItemNotNull] IEnumerable<ModuleInfo> modules)
        {
            var results = new List<ModuleInfo>();
            foreach (var module in modules)
            {
                if (module.IsSubmodule)
                {
                    results.AddRange(GetTopLevelModules(new List<ModuleInfo> { module.Parent }));
                }
                else
                {
                    results.Add(module);
                }
            }

            return results.Distinct();
        }
    }
}
