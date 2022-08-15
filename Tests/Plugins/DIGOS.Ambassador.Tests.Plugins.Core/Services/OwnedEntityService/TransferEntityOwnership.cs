//
//  TransferEntityOwnership.cs
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

using System.Collections.Generic;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using Moq;
using Remora.Rest.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

// ReSharper disable RedundantDefaultMemberInitializer - suppressions for indirectly initialized properties.
namespace DIGOS.Ambassador.Tests.Plugins.Core;

public partial class OwnedEntityServiceTests
{
    public class TransferEntityOwnershipAsync : OwnedEntityServiceTestBase
    {
        private readonly Snowflake _originalUser = new(0);
        private readonly Snowflake _newUser = new(1);

        private User _originalDBUser = null!;
        private User _newDBUser = null!;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Set up mocked database users
            _originalDBUser = (await this.Users.AddUserAsync(_originalUser)).Entity;
            _newDBUser = (await this.Users.AddUserAsync(_newUser)).Entity;
        }

        [Fact]
        public async Task ReturnsErrorIfUserAlreadyOwnsTheEntity()
        {
            // Set up entity owned by the original user
            var entityMock = new Mock<IOwnedNamedEntity>();
            entityMock.Setup(e => e.Name).Returns("Test");
            entityMock.Setup(e => e.Owner).Returns(_originalDBUser);
            entityMock.Setup
                (
                    e =>
                        e.IsOwner(It.IsAny<User>())
                )
                .Returns<User>
                (
                    u =>
                        u == entityMock.Object.Owner
                );

            // Set up the list of entities owned by the new owner
            var collection = new List<IOwnedNamedEntity> { entityMock.Object };

            var result = await this.Entities.TransferEntityOwnershipAsync(_newUser, collection, entityMock.Object);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task ReturnsErrorIfUserAlreadyOwnsAnEntityWithTheSameName()
        {
            // Set up the entities owned by the users
            var entityOwnedByOriginal = new Mock<IOwnedNamedEntity>();
            entityOwnedByOriginal.Setup(e => e.Name).Returns("Test");
            entityOwnedByOriginal.Setup(e => e.Owner).Returns(_originalDBUser);

            var entityOwnedByNew = new Mock<IOwnedNamedEntity>();
            entityOwnedByNew.Setup(e => e.Name).Returns("Test");
            entityOwnedByNew.Setup(e => e.Owner).Returns(_newDBUser);

            // Set up the list of entities owned by the new owner
            var collection = new List<IOwnedNamedEntity> { entityOwnedByNew.Object };

            var result = await this.Entities.TransferEntityOwnershipAsync(_newUser, collection, entityOwnedByOriginal.Object);
            Assert.False(result.IsSuccess);
        }

        [Fact]
        public async Task IsSuccessfulIfUserDoesNotOwnTheEntityAndDoesNotOwnAnEntityWithTheSameName()
        {
            // Set up the entities owned by the users
            var entityOwnedByOriginalMock = new Mock<IOwnedNamedEntity>();
            entityOwnedByOriginalMock.Setup(e => e.Name).Returns("Test");
            entityOwnedByOriginalMock.Setup(e => e.Owner).Returns(_originalDBUser);
            entityOwnedByOriginalMock.SetupProperty(e => e.Owner);

            var entityOwnedByNewMock = new Mock<IOwnedNamedEntity>();
            entityOwnedByNewMock.Setup(e => e.Name).Returns("Test2");
            entityOwnedByNewMock.Setup(e => e.Owner).Returns(_newDBUser);

            var entityOwnedByOriginal = entityOwnedByOriginalMock.Object;
            var entityOwnedByNew = entityOwnedByNewMock.Object;

            // Set up the list of entities owned by the new owner
            var collection = new List<IOwnedNamedEntity> { entityOwnedByNew };

            var result = await this.Entities.TransferEntityOwnershipAsync(_newUser, collection, entityOwnedByOriginal);
            Assert.True(result.IsSuccess);
            Assert.Same(_newDBUser, entityOwnedByOriginal.Owner);
        }
    }
}
