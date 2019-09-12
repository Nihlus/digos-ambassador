//
//  PatternColourShifter.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Shifters
{
    /// <summary>
    /// Shifts the pattern colour of components.
    /// </summary>
    internal sealed class PatternColourShifter : AppearanceShifter
    {
        [NotNull]
        private readonly TransformationService _transformations;

        [NotNull]
        private readonly TransformationDescriptionBuilder _descriptionBuilder;

        [NotNull]
        private readonly Colour _colour;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternColourShifter"/> class.
        /// </summary>
        /// <param name="appearance">The appearance to shift.</param>
        /// <param name="colour">The colour to shift into.</param>
        /// <param name="transformations">The transformation service.</param>
        /// <param name="descriptionBuilder">The description builder.</param>
        public PatternColourShifter
        (
            [NotNull] Appearance appearance,
            [NotNull] Colour colour,
            [NotNull] TransformationService transformations,
            [NotNull] TransformationDescriptionBuilder descriptionBuilder
        )
            : base(appearance)
        {
            _colour = colour;
            _transformations = transformations;
            _descriptionBuilder = descriptionBuilder;
        }

        /// <inheritdoc />
        protected override async Task<ShiftBodypartResult> ShiftBodypartAsync(Bodypart bodypart, Chirality chirality)
        {
            if (!this.Appearance.TryGetAppearanceComponent(bodypart, chirality, out var currentComponent))
            {
                return ShiftBodypartResult.FromError("The character doesn't have that bodypart.");
            }

            if (currentComponent.Pattern is null)
            {
                return ShiftBodypartResult.FromError("That bodypart doesn't have a pattern.");
            }

            // ReSharper disable once PossibleNullReferenceException
            // Having a pattern implies having a pattern colour
            if (currentComponent.PatternColour.IsSameColourAs(_colour))
            {
                return ShiftBodypartResult.FromError("The pattern is already that colour.");
            }

            currentComponent.PatternColour = _colour.Clone();

            var shiftMessage = await GetShiftMessageAsync(bodypart, chirality);
            return ShiftBodypartResult.FromSuccess(shiftMessage, ShiftBodypartAction.Shift);
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformShiftMessageAsync(Bodypart bodypart)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, Chirality.Left);
            return Task.FromResult
            (
                _descriptionBuilder.BuildUniformPatternColourShiftMessage(this.Appearance, component)
            );
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformAddMessageAsync(Bodypart bodypart)
        {
            throw new InvalidOperationException("Colours can't be added.");
        }

        /// <inheritdoc />
        protected override Task<string> GetShiftMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, chirality);
            return Task.FromResult(_descriptionBuilder.BuildPatternColourShiftMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetAddMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            throw new InvalidOperationException("Colours can't be added.");
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
