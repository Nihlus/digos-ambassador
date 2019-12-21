//
//  DeleteWarningAsync.cs
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
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.WarningService
{
    public partial class WarningService
    {
        public class DeleteWarningAsync : WarningServiceTestBase
        {
            private readonly IGuildUser _guildUser = MockHelper.CreateDiscordEntity<IGuildUser>
            (
                0,
                m => m.Setup(gu => gu.Guild.Id).Returns(0)
            );

            private readonly IUser _author = MockHelper.CreateDiscordUser(1);

            [Fact]
            private async Task ReturnsUnsuccessfulIfWarningDoesNotExist()
            {
                var warning = new UserWarning(new Server(0), new User(0), new User(1), "Dummy thicc");

                var result = await this.Warnings.DeleteWarningAsync(warning);

                Assert.False(result.IsSuccess);
            }

            [Fact]
            private async Task ReturnsSuccessfulIfWarningExists()
            {
                var warning = (await this.Warnings.CreateWarningAsync(_author, _guildUser, "Dummy thicc")).Entity;

                var result = await this.Warnings.DeleteWarningAsync(warning);
                Assert.True(result.IsSuccess);
            }

            [Fact]
            private async Task ActuallyDeletesWarning()
            {
                var warning = (await this.Warnings.CreateWarningAsync(_author, _guildUser, "Dummy thicc")).Entity;

                await this.Warnings.DeleteWarningAsync(warning);

                Assert.Empty(this.Database.UserWarnings);
            }
        }
    }
}
