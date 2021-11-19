//
//  IsEntityNameValid.cs
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
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable SA1649

namespace DIGOS.Ambassador.Tests.Plugins.Core;

public partial class OwnedEntityServiceTests
{
    public class IsEntityNameValid : OwnedEntityServiceTestBase
    {
        private readonly IReadOnlyCollection<string> _commandNames;

        public IsEntityNameValid()
        {
            _commandNames = new[] { "create", "set name", "show" };
        }

        [Fact]
        public void ReturnsFailureForNullNames()
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            var result = this.Entities.IsEntityNameValid(_commandNames, null);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData(':')]
        public void ReturnsFailureIfNameContainsInvalidCharacters(char invalidCharacter)
        {
            var result = this.Entities.IsEntityNameValid(_commandNames, $"Test{invalidCharacter}");

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("current")]
        public void ReturnsFailureIfNameIsAReservedName(string reservedName)
        {
            var result = this.Entities.IsEntityNameValid(_commandNames, reservedName);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("create")]
        [InlineData("show")]
        [InlineData("character show")]
        [InlineData("create Test Testsson")]
        [InlineData("set name Amby")]
        public void ReturnsFailureIfNameContainsACommandName(string commandName)
        {
            var result = this.Entities.IsEntityNameValid(_commandNames, commandName);

            Assert.False(result.IsSuccess);
        }

        [Theory]
        [InlineData("Norm")]
        [InlineData("Tali'Zorah")]
        [InlineData("August Strindberg")]
        public void ReturnsSuccessForNormalNames(string name)
        {
            var result = this.Entities.IsEntityNameValid(_commandNames, name);

            Assert.True(result.IsSuccess);
        }
    }
}
