//
//  CreateWarningAsync.cs
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
using DIGOS.Ambassador.Tests.Plugins.Moderation.Bases;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.BanService
{
    public partial class BanService
    {
        public class CreateBanAsync : BanServiceTestBase
        {
            private readonly IGuildUser _guildUser = MockHelper.CreateDiscordEntity<IGuildUser>
            (
                0,
                m => m.Setup(gu => gu.Guild.Id).Returns(0)
            );

            private readonly IUser _author = MockHelper.CreateDiscordUser(1);

            [Fact]
            private async Task ReturnsSuccessful()
            {
                var result = await this.Bans.CreateBanAsync(_author, _guildUser, "Dummy thicc");

                Assert.True(result.IsSuccess);
            }

            [Fact]
            private async Task ActuallyCreatesBan()
            {
                await this.Bans.CreateBanAsync(_author, _guildUser, "Dummy thicc");

                Assert.NotEmpty(this.Database.UserBans);
            }

            [Fact]
            private async Task CanCreateWithExpiryDate()
            {
                var expiryDate = DateTime.Today.AddDays(1);

                var result = await this.Bans.CreateBanAsync
                (
                    _author,
                    _guildUser,
                    "Dummy thicc",
                    expiresOn: expiryDate
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(expiryDate, result.Entity.ExpiresOn);
            }

            [Fact]
            private async Task CanCreateWithMessage()
            {
                var messageID = 1;

                var result = await this.Bans.CreateBanAsync
                (
                    _author,
                    _guildUser,
                    "Dummy thicc",
                    messageID
                );

                Assert.True(result.IsSuccess);
                Assert.Equal(messageID, result.Entity.MessageID);
            }
        }
    }
}
