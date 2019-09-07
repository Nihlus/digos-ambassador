//
//  SpeciesShifter.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Shifters
{
    /// <summary>
    /// Shifts the species of appearances.
    /// </summary>
    public sealed class SpeciesShifter : AppearanceShifter
    {
        [CanBeNull]
        private readonly Species _species;

        [NotNull]
        private readonly TransformationService _transformations;

        [NotNull]
        private readonly TransformationDescriptionBuilder _descriptionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesShifter"/> class.
        /// </summary>
        /// <param name="appearance">The appearance that is being shifted.</param>
        /// <param name="species">The species to shift into.</param>
        /// <param name="transformations">The transformation service.</param>
        /// <param name="descriptionBuilder">The description builder.</param>
        public SpeciesShifter
        (
            Appearance appearance,
            Species species,
            TransformationService transformations,
            TransformationDescriptionBuilder descriptionBuilder
        )
            : base(appearance)
        {
            _species = species;
            _transformations = transformations;
            _descriptionBuilder = descriptionBuilder;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SpeciesShifter"/> class.
        /// </summary>
        /// <param name="appearance">The appearance that is being shifted.</param>
        /// <param name="transformations">The transformation service.</param>
        /// <param name="descriptionBuilder">The description builder.</param>
        public SpeciesShifter
        (
            Appearance appearance,
            TransformationService transformations,
            TransformationDescriptionBuilder descriptionBuilder
        )
            : base(appearance)
        {
            _transformations = transformations;
            _descriptionBuilder = descriptionBuilder;
        }

        /// <inheritdoc />
        protected override async Task<ShiftBodypartResult> ShiftBodypartAsync(Bodypart bodypart, Chirality chirality)
        {
            if (_species is null)
            {
                throw new InvalidOperationException
                (
                    "The shifter must be constructed with a target species when shifting parts."
                );
            }

            var character = this.Appearance.Character;

            var getTFResult = await _transformations.GetTransformationsByPartAndSpeciesAsync(bodypart, _species);
            if (!getTFResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getTFResult);
            }

            var transformation = getTFResult.Entity.First();

            var getAppearanceResult = await _transformations.GetOrCreateCurrentAppearanceAsync(character);
            if (!getAppearanceResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getAppearanceResult);
            }

            var appearance = getAppearanceResult.Entity;

            if (appearance.TryGetAppearanceComponent(bodypart, chirality, out var existingComponent))
            {
                if (existingComponent.Transformation.Species.Name.Equals(transformation.Species.Name))
                {
                    var message = await GetNoChangeMessageAsync(bodypart);

                    return ShiftBodypartResult.FromError
                    (
                        message
                    );
                }
            }

            string shiftMessage;

            if (!appearance.TryGetAppearanceComponent(bodypart, chirality, out var currentComponent))
            {
                currentComponent = AppearanceComponent.CreateFrom(transformation, chirality);

                appearance.Components.Add(currentComponent);

                shiftMessage = await GetAddMessageAsync(bodypart, chirality);
                return ShiftBodypartResult.FromSuccess(shiftMessage, ShiftBodypartAction.Add);
            }

            if (currentComponent.Transformation.Species.Name == "template")
            {
                // Apply default settings
                currentComponent.BaseColour = transformation.DefaultBaseColour.Clone();

                currentComponent.Pattern = transformation.DefaultPattern;
                currentComponent.PatternColour = transformation.DefaultPatternColour?.Clone();
            }

            currentComponent.Transformation = transformation;

            shiftMessage = await GetShiftMessageAsync(bodypart, chirality);

            return ShiftBodypartResult.FromSuccess(shiftMessage, ShiftBodypartAction.Shift);
        }

        /// <inheritdoc />
        protected override async Task<ShiftBodypartResult> RemoveBodypartAsync(Bodypart bodypart, Chirality chirality)
        {
            var character = this.Appearance.Character;

            var getAppearanceResult = await _transformations.GetOrCreateCurrentAppearanceAsync(character);
            if (!getAppearanceResult.IsSuccess)
            {
                return ShiftBodypartResult.FromError(getAppearanceResult);
            }

            var appearance = getAppearanceResult.Entity;

            if (!appearance.TryGetAppearanceComponent(bodypart, chirality, out var component))
            {
                return ShiftBodypartResult.FromError("The character doesn't have that bodypart.");
            }

            appearance.Components.Remove(component);

            var removeMessage = _descriptionBuilder.BuildRemoveMessage(appearance, bodypart);
            return ShiftBodypartResult.FromSuccess(removeMessage, ShiftBodypartAction.Remove);
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformShiftMessageAsync(Bodypart bodypart)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, Chirality.Left);
            return Task.FromResult(_descriptionBuilder.BuildUniformShiftMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformAddMessageAsync(Bodypart bodypart)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, Chirality.Left);
            return Task.FromResult(_descriptionBuilder.BuildUniformGrowMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformRemoveMessageAsync(Bodypart bodypart)
        {
            return Task.FromResult(_descriptionBuilder.BuildRemoveMessage(this.Appearance, bodypart));
        }

        /// <inheritdoc />
        protected override Task<string> GetShiftMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, chirality);
            return Task.FromResult(_descriptionBuilder.BuildShiftMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetAddMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, chirality);
            return Task.FromResult(_descriptionBuilder.BuildGrowMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetRemoveMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            return Task.FromResult(_descriptionBuilder.BuildRemoveMessage(this.Appearance, bodypart));
        }

        /// <inheritdoc />
        protected override Task<string> GetNoChangeMessageAsync(Bodypart bodypart)
        {
            var character = this.Appearance.Character;

            if (_species is null)
            {
                // TODO: Improve this, it's crap
                // Assume we're in removal mode
                return Task.FromResult($"{character.Nickname} doesn't have anything to remove.");
            }

            var bodypartHumanized = bodypart.Humanize();
            if (bodypart == Bodypart.Full)
            {
                var fullMessage = $"{character.Nickname} is already a {_species.Name.Humanize()}.";
                fullMessage = fullMessage.Transform(To.LowerCase, To.SentenceCase);

                return Task.FromResult(fullMessage);
            }

            var message =
                $"{character.Name}'s {bodypartHumanized} " +
                $"{(bodypartHumanized.EndsWith("s") ? "are" : "is")} already a {_species.Name.Humanize()}'s.";

            message = message.Transform(To.LowerCase, To.SentenceCase);
            return Task.FromResult(message);
        }
    }
}
