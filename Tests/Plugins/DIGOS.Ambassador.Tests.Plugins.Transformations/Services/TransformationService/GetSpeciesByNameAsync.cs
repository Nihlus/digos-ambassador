//
//  GetSpeciesByNameAsync.cs
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

using System.Threading.Tasks;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class GetSpeciesByNameAsync : TransformationServiceTestBase
        {
            [Fact]
            public async Task ReturnsCorrectSpeciesForGivenName()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync("template");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }

            [Fact]
            public async Task ReturnsUnsuccesfulResultForNonexistantName()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync("aasddduaiii");

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task IsCaseInsensitive()
            {
                var result = await this.Transformations.GetSpeciesByNameAsync("TEMPLATE");

                Assert.True(result.IsSuccess);
                Assert.Equal("template", result.Entity.Name);
            }
        }
    }
}
