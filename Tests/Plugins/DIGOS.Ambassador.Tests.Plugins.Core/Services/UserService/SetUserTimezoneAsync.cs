//
//  SetUserTimezoneAsync.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;
using Remora.Discord.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Core
{
    public static partial class UserServiceTests
    {
        public class SetUserTimezoneAsync : UserServiceTestBase
        {
            private User _user = null!;

            public static IEnumerable<object[]> ValidTimezoneOffsets
            {
                [UsedImplicitly]
                get
                {
                    for (var i = -12; i < 14; i++)
                    {
                        yield return new object[] { i };
                    }
                }
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                var discordUser = new Snowflake(0);
                var user = await this.Users.AddUserAsync(discordUser);
                _user = user.Entity;
            }

            [Theory]
            [MemberData(nameof(ValidTimezoneOffsets))]
            public async Task ReturnsTrueForValidTimezone(int timezone)
            {
                var result = await this.Users.SetUserTimezoneAsync(_user, timezone);

                Assert.True(result.IsSuccess);
            }

            [Theory]
            [MemberData(nameof(ValidTimezoneOffsets))]
            public async Task ActuallySetsValueForValidTimezone(int timezone)
            {
                await this.Users.SetUserTimezoneAsync(_user, timezone);

                Assert.Equal(timezone, _user.Timezone);
            }

            [Theory]
            [InlineData(-13)]
            [InlineData(15)]
            public async Task ReturnsFalseForInvalidTimezone(int timezone)
            {
                var result = await this.Users.SetUserTimezoneAsync(_user, timezone);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsFalseIfTimezoneIsAlreadySet()
            {
                const int timezone = 10;

                await this.Users.SetUserTimezoneAsync(_user, timezone);

                var result = await this.Users.SetUserTimezoneAsync(_user, timezone);

                Assert.False(result.IsSuccess);
            }
        }
    }
}
