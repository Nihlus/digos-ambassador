//
//  GetTransformationByPartAndSpeciesAsync.cs
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

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class GetTransformationByPartAndSpeciesAsync : TransformationServiceTestBase
        {
            private Species _templateSpecies;

            protected override async Task InitializeTestAsync()
            {
                _templateSpecies = await this.Database.Species.FirstAsync(s => s.Name == "template");
            }

            [Fact]
            public async Task RetrievesCorrectBodypart()
            {
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    Bodypart.Face,
                    _templateSpecies
                );

                Assert.True(result.IsSuccess);
                Assert.Single(result.Entity);

                var transformation = result.Entity.First();

                Assert.Equal(Bodypart.Face, transformation.Part);
                Assert.Same(_templateSpecies, transformation.Species);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSpeciesDoesNotExist()
            {
                var nonexistantSpecies = new Species("ooga", "Dummy", "booga");
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    Bodypart.Face,
                    nonexistantSpecies
                );

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfCombinationDoesNotExist()
            {
                var result = await this.Transformations.GetTransformationsByPartAndSpeciesAsync
                (
                    Bodypart.Wings,
                    _templateSpecies
                );

                Assert.False(result.IsSuccess);
            }
        }
    }
}
