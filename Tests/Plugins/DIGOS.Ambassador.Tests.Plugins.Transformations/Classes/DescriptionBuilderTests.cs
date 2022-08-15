//
//  DescriptionBuilderTests.cs
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

using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Model.Servers;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages;
using Microsoft.Extensions.DependencyInjection;
using Remora.Rest.Core;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591
#pragma warning disable CS8625

namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

public class DescriptionBuilderTests
{
    private const string _sampleFluentText = "{@f|They have} long {@colour} hair. {@f|Their} name is {@target}. {@f|They are} a DIGOS unit.";
    private const string _expectedText = "She has long fluorescent white hair. Her name is Amby. She is a DIGOS unit.";

    [Fact]
    public void ReplacesFluentTokensCorrectly()
    {
        var character = new Character
        (
            new User(new Snowflake(0)),
            new Server(new Snowflake(0)),
            "Amby",
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            pronounProviderFamily: "Feminine"
        );

        var hairColour = new Colour(Shade.White, ShadeModifier.Fluorescent);

        var hairTransformation = new Transformation
        (
            new Species("dummy", "dummy", "dummy"),
            "dummy",
            hairColour,
            "dummy",
            "dummy",
            _sampleFluentText
        )
        {
            Part = Bodypart.Hair
        };

        var hairComponent = AppearanceComponent.CreateFrom(hairTransformation);

        var appearance = new Appearance(character);
        appearance.Components.Add(hairComponent);

        var pronounService = new PronounService();
        pronounService.WithPronounProvider(new FemininePronounProvider());

        var characterService = new CharacterService(null!, null!, null!, pronounService);

        var serviceProvider = new ServiceCollection()
            .AddSingleton(pronounService)
            .AddSingleton(characterService)
            .BuildServiceProvider();

        var descriptionBuilder = new TransformationDescriptionBuilder(serviceProvider, new TransformationText());

        var result = descriptionBuilder.ReplaceTokensWithContent(_sampleFluentText, appearance, hairComponent);

        Assert.Equal(_expectedText, result);
    }
}
