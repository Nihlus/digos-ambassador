//
//  TransformationDataProvider.cs
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

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DIGOS.Ambassador.Tests.ContentTests.Data
{
    /// <summary>
    /// Provides a feed of paths to bundled transformation files for use as test parameters.
    /// </summary>
    public class TransformationDataProvider : IEnumerable<object[]>
    {
        /// <inheritdoc />
        public IEnumerator<object[]> GetEnumerator()
        {
            var baseContentPath = Path.Combine("Content", "Transformations", "Species");
            var speciesDirectories = Directory.EnumerateDirectories(baseContentPath);

            foreach (var speciesDirectory in speciesDirectories)
            {
                var transformationFiles = Directory.EnumerateFiles(speciesDirectory)
                        .Where(p => !p.EndsWith("Species.yml"));

                foreach (var transformationFile in transformationFiles)
                {
                    yield return new object[] { transformationFile };
                }
            }
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
