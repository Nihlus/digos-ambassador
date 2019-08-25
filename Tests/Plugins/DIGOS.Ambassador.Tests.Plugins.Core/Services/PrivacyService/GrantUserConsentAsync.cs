//
//  GrantUserConsentAsync.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Tests.Utility;
using Discord;
using Microsoft.EntityFrameworkCore;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core
{
    public static partial class PrivacyServiceTests
    {
        public class GrantUserConsentAsync : PrivacyServiceTestBase
        {
            private readonly IUser _discordUser;

            public GrantUserConsentAsync()
            {
                _discordUser = MockHelper.CreateDiscordUser(0);
            }

            [Fact]
            public async Task AddsNewRecordIfUserHasNotConsentedBefore()
            {
                await this.Privacy.GrantUserConsentAsync(_discordUser);

                Assert.NotEmpty(this.Database.UserConsents);
            }

            [Fact]
            public async Task CorrectlySetsConsentIfUserHasNotConsentedBefore()
            {
                await this.Privacy.GrantUserConsentAsync(_discordUser);

                var consent = await this.Database.UserConsents.FirstOrDefaultAsync();

                Assert.NotNull(consent);
                Assert.Equal((long)_discordUser.Id, consent.DiscordID);
                Assert.True(consent.HasConsented);
            }

            [Fact]
            public async Task ReusesRecordIfUserHasConsentedBefore()
            {
                await this.Privacy.GrantUserConsentAsync(_discordUser);

                var firstConsent = await this.Database.UserConsents.FirstAsync();

                await this.Privacy.RevokeUserConsentAsync(_discordUser);

                await this.Privacy.GrantUserConsentAsync(_discordUser);

                var secondConsent = await this.Database.UserConsents.FirstAsync();

                Assert.Same(firstConsent, secondConsent);

                Assert.Equal((long)_discordUser.Id, secondConsent.DiscordID);
                Assert.True(secondConsent.HasConsented);
            }
        }
    }
}
