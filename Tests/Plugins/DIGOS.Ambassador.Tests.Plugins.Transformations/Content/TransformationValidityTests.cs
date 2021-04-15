//
//  TransformationValidityTests.cs
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
using System.IO;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Xunit;

#pragma warning disable SA1600
#pragma warning disable CS1591

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    public class TransformationValidityTests : TransformationValidityTestBase
    {
        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationFileIsValid(string transformationFile)
        {
            var result = this.Verifier.VerifyFile<Transformation>(transformationFile);

            Assert.True(result.IsSuccess, result.IsSuccess ? string.Empty : result.Unwrap().Message);
        }

        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationFileIsInCorrectFolder(string transformationFile)
        {
            var folderName = Directory.GetParent(transformationFile)?.Name;
            var transformation = Deserialize<Transformation>(transformationFile);

            Assert.Equal(transformation.Species.Name, folderName);
        }

        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationFileIsCorrectlyNamed(string transformationFile)
        {
            var bodypartName = Path.GetFileNameWithoutExtension(transformationFile);
            Assert.True(Enum.TryParse<Bodypart>(bodypartName, out var bodypart), "The file name must be a valid body part.");

            var transformation = Deserialize<Transformation>(transformationFile);
            Assert.Equal(bodypart, transformation.Part);
        }

        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationTransformsNonChiralBodypart(string transformationFile)
        {
            var transformation = Deserialize<Transformation>(transformationFile);
            Assert.False(transformation.Part.IsComposite());
        }

        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationHasRequiredConditionalFields(string transformationFile)
        {
            var transformation = Deserialize<Transformation>(transformationFile);

            if (transformation.DefaultPattern is not null)
            {
                Assert.NotNull(transformation.DefaultPatternColour);
            }

            if (transformation.Part.IsChiral())
            {
                Assert.NotNull(transformation.UniformShiftMessage);
                Assert.NotNull(transformation.UniformGrowMessage);
                Assert.NotNull(transformation.UniformDescription);
            }
            else
            {
                Assert.Null(transformation.UniformShiftMessage);
                Assert.Null(transformation.UniformGrowMessage);
                Assert.Null(transformation.UniformDescription);
            }
        }

        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationHasCorrectlyMarkedAdultStatus(string transformationFile)
        {
            var transformation = Deserialize<Transformation>(transformationFile);
            if (transformation.Part == Bodypart.Penis || transformation.Part == Bodypart.Vagina)
            {
                Assert.True(transformation.IsNSFW);
            }
        }

        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationMessagesAreCorrectlyFormatted(string transformationFile)
        {
            var transformation = Deserialize<Transformation>(transformationFile);

            Assert.True(transformation.Description.EndsWith("."), "Text fields must end with a dot.");
            Assert.EndsWith(".", transformation.SingleDescription);

            if (transformation.Part.IsChiral())
            {
                Assert.True(transformation.UniformShiftMessage?.EndsWith("."), "Text fields must end with a dot.");
                Assert.True(transformation.UniformGrowMessage?.EndsWith("."), "Text fields must end with a dot.");
                Assert.True(transformation.UniformDescription?.EndsWith("."), "Text fields must end with a dot.");
            }
            else
            {
                Assert.True(transformation.ShiftMessage.EndsWith("."), "Text fields must end with a dot.");
                Assert.True(transformation.GrowMessage.EndsWith("."), "Text fields must end with a dot.");
            }
        }

        [Theory]
        [ClassData(typeof(TransformationDataProvider))]
        public void TransformationMessagesAreShortEnough(string transformationFile)
        {
            var transformation = Deserialize<Transformation>(transformationFile);

            Assert.True(transformation.Description.Length < 1800, "Messages must be less than 1800 characters.");
            Assert.True(transformation.SingleDescription.Length < 1800);

            if (transformation.Part.IsChiral())
            {
                Assert.True(transformation.UniformShiftMessage?.Length < 1800, "Messages must be less than 1800 characters.");
                Assert.True(transformation.UniformGrowMessage?.Length < 1800, "Messages must be less than 1800 characters.");
                Assert.True(transformation.UniformDescription?.Length < 1800, "Messages must be less than 1800 characters.");
            }
            else
            {
                Assert.True(transformation.ShiftMessage.Length < 1800, "Messages must be less than 1800 characters.");
                Assert.True(transformation.GrowMessage.Length < 1800, "Messages must be less than 1800 characters.");
            }
        }
    }
}
