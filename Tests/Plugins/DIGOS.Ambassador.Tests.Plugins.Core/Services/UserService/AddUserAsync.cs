//
//  AddUserAsync.cs
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
using Remora.Rest.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core;

public static partial class UserServiceTests
{
    public class AddUserAsync : UserServiceTestBase
    {
        private readonly Snowflake _discordUser;

        public AddUserAsync()
        {
            _discordUser = new Snowflake(0);
        }

        [Fact]
        public async Task ReturnsTrueIfUserHasNotBeenRegisteredBefore()
        {
            var result = await this.Users.AddUserAsync(_discordUser);

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task ActuallyRegistersUserIfUserHasNotBeenRegisteredBefore()
        {
            await this.Users.AddUserAsync(_discordUser);

            var user = this.Database.Users.First();

            Assert.NotNull(user);
            Assert.Equal(_discordUser, user.DiscordID);
        }

        [Fact]
        public async Task ReturnsFalseIfUserHasBeenRegisteredBefore()
        {
            await this.Users.AddUserAsync(_discordUser);

            var result = await this.Users.AddUserAsync(_discordUser);

            Assert.False(result.IsSuccess);
        }
    }
}
