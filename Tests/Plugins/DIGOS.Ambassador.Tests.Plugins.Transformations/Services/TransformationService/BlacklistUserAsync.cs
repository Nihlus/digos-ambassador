//
//  BlacklistUserAsync.cs
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
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public partial class TransformationServiceTests
    {
        public class BlacklistUserAsync : TransformationServiceTestBase
        {
            private readonly IUser _user = MockHelper.CreateDiscordUser(0);
            private readonly IUser _blacklistedUser = MockHelper.CreateDiscordUser(1);

            [Fact]
            public async Task CanBlacklistUser()
            {
                var result = await this.Transformations.BlacklistUserAsync(_user, _blacklistedUser);

                Assert.True(result.IsSuccess);
                Assert.Equal(ModifyEntityAction.Edited, result.ActionTaken);

                Assert.NotEmpty(this.Database.GlobalUserProtections.First().Blacklist);

                Assert.Equal((long)_blacklistedUser.Id, this.Database.GlobalUserProtections.First().Blacklist.First().DiscordID);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfUserIsAlreadyBlacklisted()
            {
                // Blacklist the user
                await this.Transformations.BlacklistUserAsync(_user, _blacklistedUser);

                // Then blacklist them again
                var result = await this.Transformations.BlacklistUserAsync(_user, _blacklistedUser);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulResultIfTargetUserIsInvokingUser()
            {
                var result = await this.Transformations.BlacklistUserAsync(_user, _user);

                Assert.False(result.IsSuccess);
            }
        }
    }
}
