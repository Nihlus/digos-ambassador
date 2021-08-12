//
//  AppearanceRemover.cs
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
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations.Shifters
{
    /// <summary>
    /// Abstract base class for appearance remover. This class is responsible for removing things from an appearance.
    /// </summary>
    internal abstract class AppearanceRemover : IAppearanceRemover
    {
        /// <summary>
        /// Gets the appearance that is being shifted.
        /// </summary>
        protected Appearance Appearance { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AppearanceRemover"/> class.
        /// </summary>
        /// <param name="appearance">The appearance that is being shifted.</param>
        protected AppearanceRemover(Appearance appearance)
        {
            this.Appearance = appearance;
        }

        /// <summary>
        /// Removes the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        protected abstract Task<Result<ShiftBodypartResult>> RemoveBodypartAsync(Bodypart bodypart, Chirality chirality);

        /// <summary>
        /// Gets a uniform removal message for the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart.</param>
        /// <returns>The removal message.</returns>
        protected abstract Task<string> GetUniformRemoveMessageAsync(Bodypart bodypart);

        /// <summary>
        /// Gets a removal message for the given bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <returns>The removal message.</returns>
        protected abstract Task<string> GetRemoveMessageAsync(Bodypart bodypart, Chirality chirality);

        /// <summary>
        /// Gets a message that signifies that no changes were made.
        /// </summary>
        /// <param name="bodypart">The bodypart.</param>
        /// <returns>The no-change message.</returns>
        protected abstract Task<string> GetNoChangeMessageAsync(Bodypart bodypart);

        /// <inheritdoc />
        public async Task<Result<ShiftBodypartResult>> RemoveAsync(Bodypart bodypart, Chirality chirality)
        {
            if (bodypart.IsChiral() && chirality == Chirality.Center)
            {
                return new UserError("Please specify left or right when removing one-sided bodyparts.");
            }

            if (bodypart.IsComposite())
            {
                return await RemoveCompositeBodypartAsync(bodypart);
            }

            return await RemoveBodypartAsync(bodypart, chirality);
        }

        /// <summary>
        /// Decomposes and removes the given composite bodypart.
        /// </summary>
        /// <param name="bodypart">The bodypart.</param>
        /// <returns>A shifting result which may or may not have succeeded.</returns>
        private async Task<Result<ShiftBodypartResult>> RemoveCompositeBodypartAsync(Bodypart bodypart)
        {
            var composingParts = bodypart.GetComposingParts();

            var currentParagraphLength = 0;
            var messageBuilder = new StringBuilder();
            void InsertRemovalMessage(string message)
            {
                messageBuilder.Append(message);

                if (!message.EndsWith(" "))
                {
                    messageBuilder.Append(' ');
                }

                if (currentParagraphLength > 240)
                {
                    messageBuilder.AppendLine();
                    messageBuilder.AppendLine();

                    currentParagraphLength = 0;
                }

                currentParagraphLength += message.Length;
            }

            foreach (var composingPart in composingParts)
            {
                if (composingPart.IsComposite())
                {
                    var shiftResult = await RemoveCompositeBodypartAsync(composingPart);
                    if (!shiftResult.IsSuccess || shiftResult.Entity.Action == ShiftBodypartAction.Nothing)
                    {
                        continue;
                    }

                    InsertRemovalMessage(shiftResult.Entity.ShiftMessage);
                    continue;
                }

                if (composingPart.IsChiral())
                {
                    var performLeftShift = await RemoveBodypartAsync(composingPart, Chirality.Left);
                    if (!performLeftShift.IsSuccess)
                    {
                        return performLeftShift;
                    }

                    var performRightShift = await RemoveBodypartAsync(composingPart, Chirality.Right);
                    if (!performRightShift.IsSuccess)
                    {
                        return performRightShift;
                    }

                    var leftShift = performLeftShift.Entity;
                    var rightShift = performRightShift.Entity;

                    switch (leftShift.Action)
                    {
                        // There's a couple of cases here for us to deal with.
                        // 1: both parts were removed
                        // 2: one part was removed
                        // 3: no changes were made
                        case ShiftBodypartAction.Nothing when rightShift.Action == ShiftBodypartAction.Nothing:
                        {
                            // No change, keep moving
                            continue;
                        }
                        case ShiftBodypartAction.Remove when rightShift.Action == ShiftBodypartAction.Remove:
                        {
                            var uniformShiftMessage = await GetUniformRemoveMessageAsync(composingPart);
                            InsertRemovalMessage(uniformShiftMessage);
                            continue;
                        }
                    }

                    if (leftShift.Action != ShiftBodypartAction.Nothing)
                    {
                        InsertRemovalMessage
                        (
                            await BuildMessageFromResultAsync(leftShift, composingPart, Chirality.Left)
                        );
                    }

                    if (rightShift.Action != ShiftBodypartAction.Nothing)
                    {
                        InsertRemovalMessage
                        (
                            await BuildMessageFromResultAsync(rightShift, composingPart, Chirality.Right)
                        );
                    }
                }
                else
                {
                    var performSimpleShift = await RemoveBodypartAsync
                    (
                        composingPart,
                        Chirality.Center
                    );

                    if (!performSimpleShift.IsSuccess)
                    {
                        return performSimpleShift;
                    }

                    var simpleShift = performSimpleShift.Entity;

                    if (simpleShift.Action != ShiftBodypartAction.Nothing)
                    {
                        InsertRemovalMessage
                        (
                            await BuildMessageFromResultAsync(simpleShift, composingPart, Chirality.Center)
                        );
                    }
                }
            }

            if (messageBuilder.Length == 0)
            {
                return new ShiftBodypartResult
                (
                    await GetNoChangeMessageAsync(bodypart),
                    ShiftBodypartAction.Nothing
                );
            }

            return new ShiftBodypartResult(messageBuilder.ToString(), ShiftBodypartAction.Shift);
        }

        private Task<string> BuildMessageFromResultAsync
        (
            ShiftBodypartResult result,
            Bodypart bodypart,
            Chirality chirality
        )
        {
            switch (result.Action)
            {
                case ShiftBodypartAction.Remove:
                {
                    return GetRemoveMessageAsync(bodypart, chirality);
                }
                case ShiftBodypartAction.Nothing:
                {
                    throw new InvalidOperationException("Can't build a message for something that didn't happen.");
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
