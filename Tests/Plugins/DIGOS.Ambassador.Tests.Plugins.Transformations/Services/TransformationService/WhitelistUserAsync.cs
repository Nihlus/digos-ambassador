//
//  WhitelistUserAsync.cs
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

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

using System.Linq;
using System.Threading.Tasks;
using Remora.Rest.Core;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public partial class TransformationServiceTests
{
    public class WhitelistUserAsync : TransformationServiceTestBase
    {
        private readonly Snowflake _user = new(0);
        private readonly Snowflake _whitelistedUser = new(1);

        [Fact]
        public async Task CanWhitelistUser()
        {
            var result = await this.Transformations.WhitelistUserAsync(_user, _whitelistedUser);

            Assert.True(result.IsSuccess);

            Assert.NotEmpty(this.Database.GlobalUserProtections.First().Whitelist);
            Assert.Equal(_whitelistedUser, this.Database.GlobalUserProtections.First().Whitelist.First().DiscordID);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyWhitelisted()
        {
            // Whitelist the user
            await this.Transformations.WhitelistUserAsync(_user, _whitelistedUser);

            // Then Whitelist them again
            var result = await this.Transformations.WhitelistUserAsync(_user, _whitelistedUser);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
        {
            var result = await this.Transformations.WhitelistUserAsync(_user, _user);

            Assert.False(result.IsSuccess);
        }
    }
}
