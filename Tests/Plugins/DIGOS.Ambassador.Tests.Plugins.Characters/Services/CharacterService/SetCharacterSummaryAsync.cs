//
//  SetCharacterSummaryAsync.cs
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
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649
#pragma warning disable CS8625

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Characters
{
    public partial class CharacterServiceTests
    {
        public class SetCharacterSummaryAsync : CharacterServiceTestBase
        {
            private const string Summary = "A cool person";
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);

            private User _owner = null!;
            private Character _character = null!;

            public override async Task InitializeAsync()
            {
                _owner = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

                _character = new Character(0, _owner, "Dummy")
                {
                    Summary = Summary
                };

                this.Database.Characters.Update(_character);
                this.Database.SaveChanges();
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsNull()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Characters.SetCharacterSummaryAsync(_character, null);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsEmpty()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, string.Empty);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfSummaryIsTheSameAsTheCurrentSummary()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, Summary);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfNewSummaryIsLongerThan240Characters()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, new string('a', 241));

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulResultIfSummaryIsAccepted()
            {
                var result = await this.Characters.SetCharacterSummaryAsync(_character, "Bobby");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task SetsSummary()
            {
                const string newSummary = "An uncool person";
                await this.Characters.SetCharacterSummaryAsync(_character, newSummary);

                var character = this.Database.Characters.First();
                Assert.Equal(newSummary, character.Summary);
            }
        }
    }
}
