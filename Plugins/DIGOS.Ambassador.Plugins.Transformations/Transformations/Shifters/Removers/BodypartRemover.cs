//
//  BodypartRemover.cs
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
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Shifters
{
    /// <summary>
    /// Shifts the species of appearances.
    /// </summary>
    internal sealed class BodypartRemover : AppearanceRemover
    {
        private readonly TransformationDescriptionBuilder _descriptionBuilder;

        /// <summary>
        /// Initializes a new instance of the <see cref="BodypartRemover"/> class.
        /// </summary>
        /// <param name="appearance">The appearance that is being shifted.</param>
        /// <param name="descriptionBuilder">The description builder.</param>
        public BodypartRemover
        (
            Appearance appearance,
            TransformationDescriptionBuilder descriptionBuilder
        )
            : base(appearance)
        {
            _descriptionBuilder = descriptionBuilder;
        }

        /// <inheritdoc />
        protected override async Task<Result<ShiftBodypartResult>> RemoveBodypartAsync(Bodypart bodypart, Chirality chirality)
        {
            if (!this.Appearance.TryGetAppearanceComponent(bodypart, chirality, out var component))
            {
                return new ShiftBodypartResult(await GetNoChangeMessageAsync(bodypart), ShiftBodypartAction.Nothing);
            }

            this.Appearance.Components.Remove(component);

            var removeMessage = _descriptionBuilder.BuildRemoveMessage(this.Appearance, bodypart);
            return new ShiftBodypartResult(removeMessage, ShiftBodypartAction.Remove);
        }

        /// <inheritdoc />
        protected override Task<string> GetUniformRemoveMessageAsync(Bodypart bodypart)
        {
            return Task.FromResult(_descriptionBuilder.BuildUniformRemoveMessage(this.Appearance, bodypart));
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

            return Task.FromResult($"{character.Nickname} doesn't have anything like that to remove.");
        }
    }
}
