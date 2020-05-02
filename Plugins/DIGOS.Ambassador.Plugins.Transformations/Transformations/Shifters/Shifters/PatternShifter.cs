//
//  PatternShifter.cs
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

using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using Humanizer;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Shifters
{
    /// <summary>
    /// Shifts the colour of components.
    /// </summary>
    internal sealed class PatternShifter : AppearanceShifter
    {
        private readonly TransformationDescriptionBuilder _descriptionBuilder;

        private readonly Pattern _pattern;

        private readonly Colour _patternColour;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternShifter"/> class.
        /// </summary>
        /// <param name="appearance">The appearance to shift.</param>
        /// <param name="pattern">The colour to shift into.</param>
        /// <param name="patternColour">The colour of the pattern to shift into.</param>
        /// <param name="descriptionBuilder">The description builder.</param>
        public PatternShifter
        (
            Appearance appearance,
            Pattern pattern,
            Colour patternColour,
            TransformationDescriptionBuilder descriptionBuilder
        )
            : base(appearance)
        {
            _pattern = pattern;
            _patternColour = patternColour;
            _descriptionBuilder = descriptionBuilder;
        }

        /// <inheritdoc />
        protected override async Task<ShiftBodypartResult> ShiftBodypartAsync(Bodypart bodypart, Chirality chirality)
        {
            if (!this.Appearance.TryGetAppearanceComponent(bodypart, chirality, out var currentComponent))
            {
                return ShiftBodypartResult.FromError("The character doesn't have that bodypart.");
            }

            if (currentComponent.Pattern == _pattern)
            {
                return ShiftBodypartResult.FromError("The character already has that pattern.");
            }

            var shiftAction = ShiftBodypartAction.Shift;
            if (currentComponent.Pattern is null)
            {
                shiftAction = ShiftBodypartAction.Add;
            }

            currentComponent.Pattern = _pattern;
            currentComponent.PatternColour = _patternColour.Clone();

            var shiftMessage = await GetShiftMessageAsync(bodypart, chirality);
            return ShiftBodypartResult.FromSuccess(shiftMessage, shiftAction);
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformShiftMessageAsync(Bodypart bodypart)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, Chirality.Left);
            return Task.FromResult(_descriptionBuilder.BuildUniformPatternShiftMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformAddMessageAsync(Bodypart bodypart)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, Chirality.Left);
            return Task.FromResult(_descriptionBuilder.BuildUniformPatternAddMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetShiftMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, chirality);
            return Task.FromResult(_descriptionBuilder.BuildPatternShiftMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetAddMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, chirality);
            return Task.FromResult(_descriptionBuilder.BuildPatternAddMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetNoChangeMessageAsync(Bodypart bodypart)
        {
            var character = this.Appearance.Character;

            var bodypartHumanized = bodypart.Humanize();

            if (bodypart == Bodypart.Full)
            {
                var fullMessage = $"{character.Nickname} is already that colour.";
                fullMessage = fullMessage.Transform(To.LowerCase, To.SentenceCase);

                return Task.FromResult(fullMessage);
            }

            var message =
                $"{character.Nickname}'s {bodypartHumanized} " +
                $"{(bodypartHumanized.EndsWith("s") ? "are" : "is")} already that colour.";

            message = message.Transform(To.LowerCase, To.SentenceCase);
            return Task.FromResult(message);
        }
    }
}
