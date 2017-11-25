//
//  TokenizerTests.cs
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

using DIGOS.Ambassador.Transformations;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests
{
	public class TokenizerTests
	{
		private const string TokenWithoutData = "@target";
		private const string TokenWithoutOptionalData = "@possessive";
		private const string TokenWithOptionalData = "@possessive|verb";

		private const string SampleText = "lorem ipsum {@target} dolor {@possessive} sit amet {@possessive|verb}";

		[Fact]
		public void CanParseTokenWithoutData()
		{
			var tokenizer = new TransformationTextTokenizer();
			tokenizer.WithTokenType<TargetToken>();

			Assert.NotNull(tokenizer.ParseToken(0, TokenWithoutData));
		}

		[Fact]
		public void CanParseTokenWithoutOptionalData()
		{
		}

		[Fact]
		public void CanParseTokenWithOptionalData()
		{
		}

		[Fact]
		public void CanTokenizeText()
		{
		}
	}
}
