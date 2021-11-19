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
using Remora.Discord.Core;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Moderation.Services.WarningService;

public partial class WarningService
{
    public class CreateWarningAsync : WarningServiceTestBase
    {
        private readonly Snowflake _user = new(0);
        private readonly Snowflake _guild = new(1);

        private readonly Snowflake _author = new(1);

        [Fact]
        private async Task ReturnsSuccessful()
        {
            var result = await this.Warnings.CreateWarningAsync(_author, _user, _guild, "Dummy thicc");

            Assert.True(result.IsSuccess);
        }

        [Fact]
        private async Task ActuallyCreatesWarning()
        {
            await this.Warnings.CreateWarningAsync(_author, _user, _guild, "Dummy thicc");

            Assert.NotEmpty(this.Database.UserWarnings);
        }

        [Fact]
        private async Task CanCreateWithExpiryDate()
        {
            var expiryDate = DateTimeOffset.UtcNow.Date.AddDays(1);

            var result = await this.Warnings.CreateWarningAsync
            (
                _author,
                _user,
                _guild,
                "Dummy thicc",
                expiresOn: expiryDate
            );

            Assert.True(result.IsSuccess);
            Assert.Equal(expiryDate, result.Entity.ExpiresOn);
        }

        [Fact]
        private async Task CanCreateWithMessage()
        {
            var messageID = new Snowflake(1);

            var result = await this.Warnings.CreateWarningAsync
            (
                _author,
                _user,
                _guild,
                "Dummy thicc",
                messageID
            );

            Assert.True(result.IsSuccess);
            Assert.Equal(messageID, result.Entity.MessageID);
        }
    }
}
