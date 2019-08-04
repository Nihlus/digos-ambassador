//
//  DescriptionBuilderTests.cs
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
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Database.Transformations.Appearances;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Transformations;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests
{
    public class DescriptionBuilderTests
    {
        private const string SampleFluentText = "{@f|They have} long {@colour} hair. {@f|Their} name is {@target}. {@f|They are} a DIGOS unit.";
        private const string ExpectedText = "She has long fluorescent white hair. Her name is Amby. She is a DIGOS unit.";

        [Fact]
        public void ReplacesFluentTokensCorrectly()
        {
            var hairColour = new Colour
            {
                Shade = Shade.White,
                Modifier = ShadeModifier.Fluorescent
            };

            var hairTransformation = new Transformation
            {
                DefaultBaseColour = hairColour,
                Part = Bodypart.Hair,
                SingleDescription = SampleFluentText
            };

            var hairComponent = AppearanceComponent.CreateFrom(hairTransformation);

            var appearance = new Appearance
            {
                Components = new List<AppearanceComponent> { hairComponent }
            };

            var character = new Character
            {
                Name = "Amby",
                PronounProviderFamily = "Feminine"
            };

            var appearanceConfiguration = new AppearanceConfiguration
            {
                Character = character,
                CurrentAppearance = appearance
            };

            var characterService = new CharacterService(null, null, null, null, null, null, null)
                .WithPronounProvider(new FemininePronounProvider());

            var serviceProvider = new ServiceCollection()
                .AddSingleton(characterService)
                .BuildServiceProvider();

            var descriptionBuilder = new TransformationDescriptionBuilder(serviceProvider);

            var result = descriptionBuilder.ReplaceTokensWithContent(SampleFluentText, appearanceConfiguration, hairComponent);

            Assert.Equal(ExpectedText, result);
        }
    }
}
