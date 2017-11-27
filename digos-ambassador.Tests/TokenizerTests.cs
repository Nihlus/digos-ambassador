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

using System;
using System.Linq;
using DIGOS.Ambassador.Transformations;
using Moq;
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

		private readonly IServiceProvider Services;

		public TokenizerTests()
		{
			var serviceMock = new Mock<IServiceProvider>();

			this.Services = serviceMock.Object;
		}

		[Fact]
		public void CanParseTokenWithoutData()
		{
			var tokenizer = new TransformationTextTokenizer(this.Services);
			tokenizer.WithTokenType<TargetToken>();

			Assert.NotNull(tokenizer.ParseToken(0, TokenWithoutData));
		}

		[Fact]
		public void CanParseTokenWithoutOptionalData()
		{
			var tokenizer = new TransformationTextTokenizer(this.Services);
			tokenizer.WithTokenType<PossessivePronounToken>();

			var token = tokenizer.ParseToken(0, TokenWithoutOptionalData) as PossessivePronounToken;

			Assert.NotNull(token);
			Assert.False(token.UseVerb);
		}

		[Fact]
		public void CanParseTokenWithOptionalData()
		{
			var tokenizer = new TransformationTextTokenizer(this.Services);
			tokenizer.WithTokenType<PossessivePronounToken>();

			var token = tokenizer.ParseToken(0, TokenWithOptionalData) as PossessivePronounToken;

			Assert.NotNull(token);
			Assert.True(token.UseVerb);
		}

		[Fact]
		public void CanTokenizeText()
		{
			var tokenizer = new TransformationTextTokenizer(this.Services)
				.WithTokenType<TargetToken>()
				.WithTokenType<PossessivePronounToken>();

			var tokens = tokenizer.GetTokens(SampleText);

			Assert.Equal(3, tokens.Count);

			Assert.IsType<TargetToken>(tokens.First());
			Assert.IsType<PossessivePronounToken>(tokens[1]);
			Assert.IsType<PossessivePronounToken>(tokens[2]);
		}

		[Fact]
		public void ParsesTokenStartIndexCorrectly()
		{
			var tokenizer = new TransformationTextTokenizer(this.Services)
				.WithTokenType<TargetToken>()
				.WithTokenType<PossessivePronounToken>();

			var tokens = tokenizer.GetTokens(SampleText);

			Assert.Equal(12, tokens.First().Start);

			Assert.Equal(28, tokens[1].Start);

			Assert.Equal(51, tokens[2].Start);
		}

		[Fact]
		public void ParsesTokenLengthCorrectly()
		{
			var tokenizer = new TransformationTextTokenizer(this.Services)
				.WithTokenType<TargetToken>()
				.WithTokenType<PossessivePronounToken>();

			var tokens = tokenizer.GetTokens(SampleText);

			Assert.Equal(9, tokens.First().Length);

			Assert.Equal(13, tokens[1].Length);

			Assert.Equal(18, tokens[2].Length);
		}
	}
}
