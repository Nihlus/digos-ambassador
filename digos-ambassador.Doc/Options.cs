//
//  Options.cs
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
using CommandLine;

namespace DIGOS.Ambassador.Doc
{
    /// <summary>
    /// Holds CLI options for the program.
    /// </summary>
    public class Options
    {
        /// <summary>
        /// Gets or sets the path to the assembly that documentation should be generated from.
        /// </summary>
        [Option('i', "assemblies", Required = true)]
        public IEnumerable<string> AssemblyPaths { get; set; }

        /// <summary>
        /// Gets or sets the path where documentation files should be emitted.
        /// </summary>
        [Option('o', "output", Required = true)]
        public string OutputPath { get; set; }
    }
}
