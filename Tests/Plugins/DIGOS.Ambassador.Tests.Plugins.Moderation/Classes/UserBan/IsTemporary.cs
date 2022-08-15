//
//  IsTemporary.cs
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

using System;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Remora.Rest.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Classes.UserBan;

public class UserBanTests
{
    public class IsTemporary
    {
        [Fact]
        public void ReturnsFalseIfExpiryDateIsUnset()
        {
            var instance = new Ambassador.Plugins.Moderation.Model.UserBan
            (
                new Server(new Snowflake(0)),
                new User(new Snowflake(0)),
                new User(new Snowflake(1)),
                string.Empty
            );

            Assert.False(instance.ExpiresOn.HasValue);
        }

        [Fact]
        public void ReturnsTrueIfExpiryDateIsSet()
        {
            var instance = new Ambassador.Plugins.Moderation.Model.UserBan
            (
                new Server(new Snowflake(0)),
                new User(new Snowflake(0)),
                new User(new Snowflake(1)),
                string.Empty,
                expiresOn: DateTimeOffset.UtcNow
            );

            Assert.True(instance.ExpiresOn.HasValue);
        }
    }
}
