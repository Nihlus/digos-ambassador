//
//  SetCharacterPronounAsync.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649
#pragma warning disable CS8625

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters;

public partial class CharacterServiceTests
{
    public class SetCharacterPronounsAsync : CharacterServiceTestBase
    {
        private const string _pronounFamily = "Feminine";

        private readonly Character _character;

        public SetCharacterPronounsAsync()
        {
            this.Services.GetRequiredService<PronounService>().WithPronounProvider(new FemininePronounProvider());
            this.Services.GetRequiredService<PronounService>().WithPronounProvider(new ZeHirPronounProvider());

            _character = CreateCharacter(pronouns: _pronounFamily);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfPronounIsEmpty()
        {
            var result = await this.CharacterEditor.SetCharacterPronounsAsync(_character, string.Empty);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfPronounIsTheSameAsTheCurrentPronoun()
        {
            var result = await this.CharacterEditor.SetCharacterPronounsAsync(_character, _pronounFamily);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfNoMatchingPronounProviderIsFound()
        {
            var result = await this.CharacterEditor.SetCharacterPronounsAsync(_character, "ahwooooga");

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsSuccessfulResultIfPronounIsAccepted()
        {
            var result = await this.CharacterEditor.SetCharacterPronounsAsync(_character, "Ze and hir");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task SetsPronoun()
        {
            const string newPronounFamily = "Ze and hir";
            await this.CharacterEditor.SetCharacterPronounsAsync(_character, newPronounFamily);

            var character = this.Database.Characters.First();
            Assert.Equal(newPronounFamily, character.PronounProviderFamily);
        }
    }
}
