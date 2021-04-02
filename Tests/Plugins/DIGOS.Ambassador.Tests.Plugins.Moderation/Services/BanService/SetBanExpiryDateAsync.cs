//
//  SetBanExpiryDateAsync.cs
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

using System;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Tests.Plugins.Moderation.Bases;
using Remora.Discord.Core;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.BanService
{
    public partial class BanService
    {
        public class SetBanExpiryDateAsync : BanServiceTestBase
        {
            private readonly UserBan _ban = new UserBan
            (
                new Server(new Snowflake(0)),
                new User(new Snowflake(0)),
                new User(new Snowflake(1)),
                string.Empty
            );

            [Fact]
            public async Task ReturnsUnsuccessfulIfNewExpiryDateIsSameAsExistingExpiryDate()
            {
                await this.Bans.SetBanExpiryDateAsync(_ban, DateTime.Today.AddDays(1));
                var result = await this.Bans.SetBanExpiryDateAsync(_ban, DateTime.Today.AddDays(1));

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsUnsuccessfulIfNewExpiryDateIsInThePast()
            {
                // ReSharper disable once AssignNullToNotNullAttribute
                var result = await this.Bans.SetBanExpiryDateAsync(_ban, DateTime.Today.AddDays(-1));

                Assert.False(result.IsSuccess);
            }

            [Fact]
            public async Task ReturnsSuccessfulIfNewExpiryDateIsWellFormed()
            {
                var result = await this.Bans.SetBanExpiryDateAsync(_ban, DateTime.Today.AddDays(2));

                Assert.True(result.IsSuccess);
            }

            [Fact]
            public async Task ActuallySetsExpiryDate()
            {
                var expiryDate = DateTime.Today.AddDays(1);

                await this.Bans.SetBanExpiryDateAsync(_ban, expiryDate);

                Assert.Equal(expiryDate, _ban.ExpiresOn);
            }

            [Fact]
            public async Task SetterUpdatesTimestamp()
            {
                var before = _ban.UpdatedAt;

                var expiryDate = DateTime.Today.AddDays(1);
                await this.Bans.SetBanExpiryDateAsync(_ban, expiryDate);

                var after = _ban.UpdatedAt;

                Assert.True(before < after);
            }
        }
    }
}
