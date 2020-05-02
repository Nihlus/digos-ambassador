//
//  PatternRemover.cs
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
    internal sealed class PatternRemover : AppearanceRemover
    {
        private readonly TransformationDescriptionBuilder _descriptionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatternRemover"/> class.
        /// </summary>
        /// <param name="appearance">The appearance to shift.</param>
        /// <param name="descriptionBuilder">The description builder.</param>
        public PatternRemover
        (
            Appearance appearance,
            TransformationDescriptionBuilder descriptionBuilder
        )
            : base(appearance)
        {
            _descriptionBuilder = descriptionBuilder;
        }

        /// <inheritdoc />
        protected override async Task<ShiftBodypartResult> RemoveBodypartAsync(Bodypart bodypart, Chirality chirality)
        {
            if (!this.Appearance.TryGetAppearanceComponent(bodypart, chirality, out var currentComponent))
            {
                return ShiftBodypartResult.FromError("The character doesn't have that bodypart.");
            }

            if (currentComponent.Pattern is null)
            {
                return ShiftBodypartResult.FromError("The character doesn't have a pattern on that part.");
            }

            currentComponent.Pattern = null;
            currentComponent.PatternColour = null;

            var shiftMessage = await GetRemoveMessageAsync(bodypart, chirality);
            return ShiftBodypartResult.FromSuccess(shiftMessage, ShiftBodypartAction.Remove);
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformRemoveMessageAsync(Bodypart bodypart)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, Chirality.Left);
            return Task.FromResult(_descriptionBuilder.BuildUniformPatternRemoveMessage(this.Appearance, component));
        }

        /// <inheritdoc />
        protected override Task<string> GetRemoveMessageAsync(Bodypart bodypart, Chirality chirality)
        {
            var component = this.Appearance.GetAppearanceComponent(bodypart, chirality);
            return Task.FromResult(_descriptionBuilder.BuildPatternRemoveMessage(this.Appearance, component));
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
                $"{character.Name}'s {bodypartHumanized} " +
                $"{(bodypartHumanized.EndsWith("s") ? "are" : "is")} already that colour.";

            message = message.Transform(To.LowerCase, To.SentenceCase);
            return Task.FromResult(message);
        }
    }
}
