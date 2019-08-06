//
//  PronounServiceTests.cs
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Tests.TestBases;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.ServiceTests
{
    /// <summary>
    /// Tests for the pronoun service.
    /// </summary>
    public class PronounServiceTests
    {
        public class WithPronounProvider : PronounServiceTestBase
        {
            [Fact]
            public void AddsCorrectProvider()
            {
                var provider = new TheyPronounProvider();
                this.Pronouns.WithPronounProvider(provider);

                Assert.Collection(this.Pronouns.GetAvailablePronounProviders(), p => Assert.Same(provider, p));
            }
        }

        public class GetPronounProvider : PronounServiceTestBase
        {
            private readonly Character _character;

            public GetPronounProvider()
            {
                _character = new Character
                {
                    PronounProviderFamily = new TheyPronounProvider().Family
                };
            }

            [Fact]
            public void ThrowsIfNoMatchingProviderIsFound()
            {
                Assert.Throws<KeyNotFoundException>(() => this.Pronouns.GetPronounProvider(_character));
            }

            [Fact]
            public void ReturnsCorrectProvider()
            {
                var expected = new TheyPronounProvider();
                this.Pronouns.WithPronounProvider(expected);

                var actual = this.Pronouns.GetPronounProvider(_character);

                Assert.Same(expected, actual);
            }
        }

        public class GetAvailablePronounProviders : PronounServiceTestBase
        {
            [Fact]
            public void ReturnsEmptySetWhenNoProvidersHaveBeenAdded()
            {
                Assert.Empty(this.Pronouns.GetAvailablePronounProviders());
            }

            [Fact]
            public void ReturnsNonEmptySetWhenProvidersHaveBeenAdded()
            {
                var provider = new TheyPronounProvider();
                this.Pronouns.WithPronounProvider(provider);

                Assert.NotEmpty(this.Pronouns.GetAvailablePronounProviders());
            }
        }
    }
}
