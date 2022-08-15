//
//  GetOrCreateServerUserProtectionAsync.cs
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

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Remora.Rest.Core;
using Xunit;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public partial class TransformationServiceTests
{
    public class GetOrCreateServerUserProtectionAsync : TransformationServiceTestBase
    {
        private readonly Snowflake _user = new(0);
        private readonly Snowflake _guild = new(1);

        [Fact]
        public async Task CreatesObjectIfOneDoesNotExist()
        {
            Assert.Empty(this.Database.ServerUserProtections);

            var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
            (
                _user,
                _guild
            );

            Assert.NotEmpty(this.Database.ServerUserProtections);
            Assert.Same(result.Entity, this.Database.ServerUserProtections.First());
        }

        [Fact]
        public async Task CreatedObjectIsBoundToTheCorrectServer()
        {
            var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
            (
                _user,
                _guild
            );

            Assert.Equal(_guild, result.Entity.Server.DiscordID);
        }

        [Fact]
        public async Task CreatedObjectIsBoundToTheCorrectUser()
        {
            var result = await this.Transformations.GetOrCreateServerUserProtectionAsync
            (
                _user,
                _guild
            );

            Assert.Equal(_user, result.Entity.User.DiscordID);
        }

        [Fact]
        public async Task RetrievesCorrectObjectIfOneExists()
        {
            // Create an object
            var created = await this.Transformations.GetOrCreateServerUserProtectionAsync
            (
                _user,
                _guild
            );

            // Get it from the database
            var retrieved = await this.Transformations.GetOrCreateServerUserProtectionAsync
            (
                _user,
                _guild
            );

            Assert.Same(created.Entity, retrieved.Entity);
        }

        [Fact]
        public async Task CreatedObjectRespectsGlobalDefaults()
        {
            var user = (await this.Users.GetOrRegisterUserAsync(_user)).Entity;

            var globalSetting = new GlobalUserProtection(user)
            {
                DefaultOptIn = true,
                DefaultType = ProtectionType.Whitelist
            };

            this.Database.GlobalUserProtections.Update(globalSetting);
            await this.Database.SaveChangesAsync();

            var localSetting = await this.Transformations.GetOrCreateServerUserProtectionAsync
            (
                _user,
                _guild
            );

            Assert.Equal(globalSetting.DefaultOptIn, localSetting.Entity.HasOptedIn);
            Assert.Equal(globalSetting.DefaultType, localSetting.Entity.Type);
            Assert.Same(globalSetting.User, localSetting.Entity.User);
        }
    }
}
