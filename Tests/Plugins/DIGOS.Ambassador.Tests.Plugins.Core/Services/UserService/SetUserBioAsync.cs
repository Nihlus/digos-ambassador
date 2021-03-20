//
//  SetUserBioAsync.cs
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
        public class SetUserBioAsync : UserServiceTestBase
        {
            private User _user = null!;

            public static IEnumerable<object?[]> InvalidBios
            {
                [UsedImplicitly]
                get
                {
                    yield return new object?[] { null };
                    yield return new object?[] { string.Empty };
                    yield return new object?[] { "    " };
                    yield return new object?[] { new string('a', 1025) };
                }
            }

            public override async Task InitializeAsync()
            {
                await base.InitializeAsync();

                var discordUser = new Snowflake(0);
                var user = await this.Users.AddUserAsync(discordUser);
                _user = user.Entity!;
            }

            [Fact]
            public async Task ReturnsTrueForValidBio()
            {
                var result = await this.Users.SetUserBioAsync(_user, "I'm a little teapot, short and stout.");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ActuallySetsValueForValidBio()
            {
                const string bio = "Here is my handle, here is my spout.";
                await this.Users.SetUserBioAsync(_user, bio);

                Assert.Equal(bio, _user.Bio);
            }

            [Theory]
            [MemberData(nameof(InvalidBios))]
            public async Task ReturnsFalseForInvalidBio(string bio)
            {
                var result = await this.Users.SetUserBioAsync(_user, bio);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsFalseIfBioIsAlreadySet()
            {
                const string bio = "I prefer kalashnikov.";

                await this.Users.SetUserBioAsync(_user, bio);

                var result = await this.Users.SetUserBioAsync(_user, bio);

                Assert.False(result.IsSuccess);
            }
        }
    }
}
