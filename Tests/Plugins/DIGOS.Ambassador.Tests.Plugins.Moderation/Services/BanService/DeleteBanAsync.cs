//
//  DeleteBanAsync.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model;
using DIGOS.Ambassador.Tests.Plugins.Moderation.Bases;
using Remora.Discord.Core;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.BanService;

public partial class BanService
{
    public class DeleteBanAsync : BanServiceTestBase
    {
        private readonly Snowflake _user = new(0);
        private readonly Snowflake _guild = new(1);

        private readonly Snowflake _author = new(1);

        [Fact]
        private async Task ReturnsUnsuccessfulIfBanDoesNotExist()
        {
            var ban = new UserBan(new Server(new Snowflake(0)), new User(new Snowflake(0)), new User(new Snowflake(1)), "Dummy thicc");

            var result = await this.Bans.DeleteBanAsync(ban);

            Assert.False(result.IsSuccess);
        }

        [Fact]
        private async Task ReturnsSuccessfulIfBanExists()
        {
            var ban = (await this.Bans.CreateBanAsync(_author, _user, _guild, "Dummy thicc")).Entity;

            var result = await this.Bans.DeleteBanAsync(ban);
            Assert.True(result.IsSuccess);
        }

        [Fact]
        private async Task ActuallyDeletesBan()
        {
            var ban = (await this.Bans.CreateBanAsync(_author, _user, _guild, "Dummy thicc")).Entity;

            await this.Bans.DeleteBanAsync(ban);

            Assert.Empty(this.Database.UserBans);
        }
    }
}
