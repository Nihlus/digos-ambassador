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

using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Mono.Cecil;

namespace DIGOS.Ambassador.Doc
{
    /// <summary>
    /// The main program class.
    /// </summary>
    internal static class Program
    {
        private static Options _options;

        private static async Task Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(r => _options = r);

            if (_options is null)
            {
                return;
            }

            var modules = _options.AssemblyPaths.Select(ModuleDefinition.ReadModule);
            var generator = new ModuleDocumentationGenerator(modules, _options.OutputPath);
            await generator.GenerateDocumentationAsync();
        }
    }
}
