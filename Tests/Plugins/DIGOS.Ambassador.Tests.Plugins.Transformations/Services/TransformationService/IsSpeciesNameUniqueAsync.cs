//
//  IsSpeciesNameUniqueAsync.cs
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
using JetBrains.Annotations;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class IsSpeciesNameUniqueAsync : TransformationServiceTestBase
        {
            [Theory]
            [InlineData("asadasdas")]
            [InlineData("yeee ewwah")]
            public async Task ReturnsTrueForUniqueName(string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(name);

                Assert.True(result);
            }

            [Theory]
            [InlineData("template")]
            public async Task ReturnsFalseForNonUniqueName(string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(name);

                Assert.False(result);
            }

            [Theory]
            [InlineData("TEMPLATE")]
            public async Task IsCaseInsensitive(string name)
            {
                var result = await this.Transformations.IsSpeciesNameUniqueAsync(name);

                Assert.False(result);
            }
        }
    }
}
