//
//  TokenizerTests.cs
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

using System;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.Transformations.Tokens;
using Moq;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public class TokenizerTests
{
    private const string _tokenWithoutData = "@target";
    private const string _tokenWithoutOptionalData = "@colour";
    private const string _tokenWithOptionalData = "@colour|pattern";

    private const string _sampleText = "lorem ipsum {@target} dolor {@colour} sit amet {@colour|base}";

    private readonly IServiceProvider _services;

    public TokenizerTests()
    {
        var serviceMock = new Mock<IServiceProvider>();

        _services = serviceMock.Object;
    }

    [Fact]
    public void CanParseTokenWithoutData()
    {
        var tokenizer = new TransformationTextTokenizer(_services);
        tokenizer.WithTokenType<TargetToken>();

        Assert.NotNull(tokenizer.ParseToken(0, _tokenWithoutData));
    }

    [Fact]
    public void CanParseTokenWithoutOptionalData()
    {
        var tokenizer = new TransformationTextTokenizer(_services);
        tokenizer.WithTokenType<ColourToken>();

        var token = tokenizer.ParseToken(0, _tokenWithoutOptionalData) as ColourToken;

        Assert.NotNull(token);
        Assert.False(token.UsePattern);
    }

    [Fact]
    public void CanParseTokenWithOptionalData()
    {
        var tokenizer = new TransformationTextTokenizer(_services);
        tokenizer.WithTokenType<ColourToken>();

        var token = tokenizer.ParseToken(0, _tokenWithOptionalData) as ColourToken;

        Assert.NotNull(token);
        Assert.True(token.UsePattern);
    }

    [Fact]
    public void CanTokenizeText()
    {
        var tokenizer = new TransformationTextTokenizer(_services)
            .WithTokenType<TargetToken>()
            .WithTokenType<ColourToken>();

        var tokens = tokenizer.GetTokens(_sampleText);

        Assert.Equal(3, tokens.Count);

        Assert.IsType<TargetToken>(tokens[0]);
        Assert.IsType<ColourToken>(tokens[1]);
        Assert.IsType<ColourToken>(tokens[2]);
    }

    [Fact]
    public void ParsesTokenStartIndexCorrectly()
    {
        var tokenizer = new TransformationTextTokenizer(_services)
            .WithTokenType<TargetToken>()
            .WithTokenType<ColourToken>();

        var tokens = tokenizer.GetTokens(_sampleText);

        Assert.Equal(12, tokens[0].Start);

        Assert.Equal(28, tokens[1].Start);

        Assert.Equal(47, tokens[2].Start);
    }

    [Fact]
    public void ParsesTokenLengthCorrectly()
    {
        var tokenizer = new TransformationTextTokenizer(_services)
            .WithTokenType<TargetToken>()
            .WithTokenType<ColourToken>();

        var tokens = tokenizer.GetTokens(_sampleText);

        Assert.Equal(9, tokens[0].Length);

        Assert.Equal(9, tokens[1].Length);

        Assert.Equal(14, tokens[2].Length);
    }
}
