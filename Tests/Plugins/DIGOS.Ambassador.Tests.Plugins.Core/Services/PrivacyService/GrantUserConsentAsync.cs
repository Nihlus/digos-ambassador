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

using System.Linq;
using System.Threading.Tasks;
using Remora.Rest.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core;

public static partial class PrivacyServiceTests
{
    public class GrantUserConsentAsync : PrivacyServiceTestBase
    {
        private readonly Snowflake _discordUser;

        public GrantUserConsentAsync()
        {
            _discordUser = new Snowflake(0);
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

            var consent = this.Database.UserConsents.First();

            Assert.NotNull(consent);
            Assert.Equal(_discordUser, consent.DiscordID);
            Assert.True(consent.HasConsented);
        }

        [Fact]
        public async Task ReusesRecordIfUserHasConsentedBefore()
        {
            await this.Privacy.GrantUserConsentAsync(_discordUser);

            var firstConsent = this.Database.UserConsents.First();

            await this.Privacy.RevokeUserConsentAsync(_discordUser);

            await this.Privacy.GrantUserConsentAsync(_discordUser);

            var secondConsent = this.Database.UserConsents.First();

            Assert.Same(firstConsent, secondConsent);

            Assert.Equal(_discordUser, secondConsent.DiscordID);
            Assert.True(secondConsent.HasConsented);
        }
    }
}
