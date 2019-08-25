//
//  SpeciesValidityTests.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Model;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public class SpeciesValidityTests : TransformationValidityTests
    {
        [Theory]
        [ClassData(typeof(SpeciesDataProvider))]
        public void SpeciesFolderHasASpeciesFile(string speciesFile)
        {
            Assert.True(File.Exists(speciesFile));
        }

        [Theory]
        [ClassData(typeof(SpeciesDataProvider))]
        public void SpeciesFileIsInCorrectFolder(string speciesFile)
        {
            var folderName = Directory.GetParent(speciesFile).Name;
            var species = Deserialize<Species>(speciesFile);

            Assert.Equal(species.Name, folderName);
        }

        [Theory]
        [ClassData(typeof(SpeciesDataProvider))]
        public void SpeciesFileIsValid(string speciesFile)
        {
            var result = this.Verifier.VerifyFile<Species>(speciesFile);

            // Guarding ErrorReason here, since it throws if the result was successful.
            Assert.True(result.IsSuccess, result.IsSuccess ? string.Empty : result.ErrorReason);
        }
    }
}
