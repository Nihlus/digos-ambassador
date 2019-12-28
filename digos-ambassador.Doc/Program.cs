//
//  Program.cs
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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using DIGOS.Ambassador.Doc.Abstractions;
using DIGOS.Ambassador.Doc.Data;
using Mono.Cecil;

namespace DIGOS.Ambassador.Doc
{
    /// <summary>
    /// The main program class.
    /// </summary>
    internal static class Program
    {
        private static Options? _options;

        private static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(r => _options = r);

            if (_options is null)
            {
                return;
            }

            // Set up the assembly resolver
            var additionalResolverPaths = _options.AssemblyPaths.Select(Path.GetDirectoryName).Distinct();
            var resolver = new DefaultAssemblyResolver();
            foreach (var additionalResolverPath in additionalResolverPaths)
            {
                resolver.AddSearchDirectory(additionalResolverPath);
            }

            var placeholderData = new PlaceholderData();

            var modules = _options.AssemblyPaths.Select
            (
                ap => ModuleDefinition.ReadModule(ap, new ReaderParameters { AssemblyResolver = resolver })
            ).ToArray();

            var placeholderDataAttributes = modules
                .Where(m => m.Assembly.HasCustomAttributes)
                .SelectMany(m => m.Assembly.CustomAttributes)
                .Where(c => c.AttributeType.FullName == typeof(PlaceholderDataAttribute).FullName);

            foreach (var placeholderDataAttribute in placeholderDataAttributes)
            {
                var dataType = ((TypeReference)placeholderDataAttribute.ConstructorArguments[0].Value).Resolve();
                var placeholders = ((CustomAttributeArgument[])placeholderDataAttribute.ConstructorArguments[1].Value)
                    .Select(a => (string)a.Value).ToArray();

                placeholderData.RegisterPlaceholderData(dataType, placeholders);
            }

            var generator = new ModuleDocumentationGenerator(modules, _options.OutputPath, placeholderData);
            await generator.GenerateDocumentationAsync();
        }
    }
}
