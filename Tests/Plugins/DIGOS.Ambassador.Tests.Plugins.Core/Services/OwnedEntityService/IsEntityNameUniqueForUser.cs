//
//  IsEntityNameUniqueForUser.cs
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

using System.Collections.Generic;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core;

public partial class OwnedEntityServiceTests
{
    public class IsEntityNameUniqueForUser : OwnedEntityServiceTestBase
    {
        [Fact]
        public void ReturnsTrueForEmptySet()
        {
            var entityMock = new Mock<IOwnedNamedEntity>();
            entityMock.Setup(e => e.Name).Returns("Test");

            var result = this.Entities.IsEntityNameUniqueForUser(new List<IOwnedNamedEntity>(), "Test2");

            Assert.True(result);
        }

        [Fact]
        public void ReturnsTrueForUniqueName()
        {
            var entityMock = new Mock<IOwnedNamedEntity>();
            entityMock.Setup(e => e.Name).Returns("Test");

            var collection = new List<IOwnedNamedEntity> { entityMock.Object };
            var result = this.Entities.IsEntityNameUniqueForUser(collection, "Test2");

            Assert.True(result);
        }

        [Fact]
        public void ReturnsFalseForNonUniqueName()
        {
            var entityMock = new Mock<IOwnedNamedEntity>();
            entityMock.Setup(e => e.Name).Returns("Test");

            var collection = new List<IOwnedNamedEntity> { entityMock.Object };
            var result = this.Entities.IsEntityNameUniqueForUser(collection, "Test");

            Assert.False(result);
        }

        [Fact]
        public void ReturnsFalseForNonUniqueNameAndIsCaseInsensitive()
        {
            var entityMock = new Mock<IOwnedNamedEntity>();
            entityMock.Setup(e => e.Name).Returns("Test");

            var collection = new List<IOwnedNamedEntity> { entityMock.Object };
            var result = this.Entities.IsEntityNameUniqueForUser(collection, "TEST");

            Assert.False(result);
        }
    }
}
