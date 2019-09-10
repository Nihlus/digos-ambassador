//
//  TransformationDescriptionBuilder.cs
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
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Attributes;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations
{
    /// <summary>
    /// Service class for building user-visible descriptions of characters based on their appearances.
    /// </summary>
    public sealed class TransformationDescriptionBuilder
    {
        [NotNull]
        private readonly TransformationTextTokenizer _tokenizer;

        [NotNull] private readonly Regex _sentenceSpacingRegex = new Regex
        (
            "(?<=\\w)\\.(?=\\w)",
            RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.IgnoreCase
        );

        [NotNull]
        private readonly TransformationText _transformationText;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationDescriptionBuilder"/> class.
        /// </summary>
        /// <param name="services">The available services.</param>
        /// <param name="transformationText">The content service.</param>
        public TransformationDescriptionBuilder
        (
            [NotNull] IServiceProvider services,
            [NotNull] TransformationText transformationText
        )
        {
            _tokenizer = new TransformationTextTokenizer(services);
            _tokenizer.DiscoverAvailableTokens();

            _transformationText = transformationText;
        }

        /// <summary>
        /// Replaces tokens in a piece of text with their respective contents.
        /// </summary>
        /// <param name="text">The text to replace in.</param>
        /// <param name="appearance">The character and appearance for which the text should be valid.</param>
        /// <param name="component">The transformation that the text belongs to.</param>
        /// <returns>A string with no tokens in it.</returns>
        [NotNull]
        public string ReplaceTokensWithContent
        (
            [NotNull] string text,
            [NotNull] Appearance appearance,
            [CanBeNull] AppearanceComponent component
        )
        {
            var tokens = _tokenizer.GetTokens(text);
            var tokenContentMap = tokens.ToDictionary(token => token, token => token.GetText(appearance, component));

            var relativeOffset = 0;
            var sb = new StringBuilder(text);

            foreach (var (token, content) in tokenContentMap)
            {
                sb.Remove(token.Start + relativeOffset, token.Length);
                sb.Insert(token.Start + relativeOffset, content);

                relativeOffset += content.Length - token.Length;
            }

            var result = string.Join
            (
                ". ",
                sb.ToString().Split('.').Select(s => s.Trim().Transform(To.SentenceCase))
            ).Trim();

            return result;
        }

        /// <summary>
        /// Builds a complete visual description of the given character.
        /// </summary>
        /// <param name="appearance">The appearance to describe.</param>
        /// <returns>A visual description of the character.</returns>
        [Pure, NotNull]
        public string BuildVisualDescription([NotNull] Appearance appearance)
        {
            var sb = new StringBuilder();

            var sexSpecies = _transformationText.Descriptions.SexSpecies.PickRandom();

            sb.Append(ReplaceTokensWithContent(sexSpecies, appearance, null));
            sb.AppendLine();
            sb.AppendLine();

            var speciesPartsToSkip = new List<AppearanceComponent>();
            var patternPartsToSkip = new List<AppearanceComponent>();
            var componentCount = 0;

            var orderedComponents = appearance.Components.OrderByDescending
            (
                c =>
                {
                    var priorityAttribute = c.Bodypart.GetCustomAttribute<DescriptionPriorityAttribute>();

                    return priorityAttribute?.Priority ?? 0;
                }
            );

            foreach (var component in orderedComponents)
            {
                ++componentCount;

                // Break the description into paragraphs every third component
                // TODO: Improve this with a true paragrapher
                if (componentCount % 3 == 0)
                {
                    sb.Append("\n\n");
                }

                var csb = new StringBuilder();

                var transformation = component.Transformation;
                if (component.Bodypart.IsChiral())
                {
                    if (!speciesPartsToSkip.Contains(component))
                    {
                        var sameSpecies = AreChiralPartsTheSameSpecies(appearance, component);
                        csb.Append
                        (
                            sameSpecies
                                ? transformation.UniformDescription
                                : transformation.SingleDescription
                        );

                        if (sameSpecies)
                        {
                            if
                            (
                                appearance.TryGetAppearanceComponent
                                (
                                    component.Bodypart,
                                    component.Chirality.Opposite(),
                                    out var partToSkip
                                )
                            )
                            {
                                speciesPartsToSkip.Add(partToSkip);
                            }
                        }
                    }

                    if (!patternPartsToSkip.Contains(component))
                    {
                        var samePattern = DoChiralPartsHaveTheSamePattern(appearance, component);
                        csb.Append
                        (
                            samePattern
                                ? _transformationText.Descriptions.Uniform.Pattern.PickRandom()
                                : _transformationText.Descriptions.Single.Pattern.PickRandom()
                        );

                        if (samePattern)
                        {
                            if
                            (
                                appearance.TryGetAppearanceComponent
                                (
                                    component.Bodypart,
                                    component.Chirality.Opposite(),
                                    out var partToSkip
                                )
                            )
                            {
                                patternPartsToSkip.Add(partToSkip);
                            }
                        }
                    }
                }
                else
                {
                    csb.Append(transformation.SingleDescription);

                    if (component.Pattern.HasValue)
                    {
                        var patternDescription = _transformationText.Descriptions.Single.Pattern.PickRandom();
                        csb.Append(patternDescription);
                    }
                }

                var tokenizedDesc = csb.ToString();
                var componentDesc = ReplaceTokensWithContent(tokenizedDesc, appearance, component);

                sb.Append(componentDesc);
            }

            var description = sb.ToString().Trim();
            var withSentenceSpacing = _sentenceSpacingRegex.Replace(description, ". ");

            return withSentenceSpacing;
        }

        /// <summary>
        /// Determines if the chiral parts on the character are the same species.
        /// </summary>
        /// <param name="appearanceConfiguration">The character and its appearances.</param>
        /// <param name="component">The chiral component.</param>
        /// <returns>true if the parts are the same species; otherwise, false.</returns>
        private bool AreChiralPartsTheSameSpecies
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            if
            (
                !appearanceConfiguration.TryGetAppearanceComponent
                (
                    component.Bodypart,
                    component.Chirality.Opposite(),
                    out var opposingComponent
                )
            )
            {
                return false;
            }

            return string.Equals(component.Transformation.Species.Name, opposingComponent.Transformation.Species.Name);
        }

        /// <summary>
        /// Determines if the chiral parts on the character have the same pattern.
        /// </summary>
        /// <param name="appearance">The appearance.</param>
        /// <param name="component">The chiral component.</param>
        /// <returns>true if the parts have the same pattern; otherwise, false.</returns>
        private bool DoChiralPartsHaveTheSamePattern
        (
            [NotNull] Appearance appearance,
            [NotNull] AppearanceComponent component
        )
        {
            if
            (
                !appearance.TryGetAppearanceComponent
                (
                    component.Bodypart,
                    component.Chirality.Opposite(),
                    out var opposingComponent
                )
            )
            {
                return false;
            }

            if (component.Pattern != opposingComponent.Pattern)
            {
                return false;
            }

            if (component.PatternColour is null ^ component.PatternColour is null)
            {
                return false;
            }

            if (component.PatternColour is null && opposingComponent.PatternColour is null)
            {
                return true;
            }

            if (component.PatternColour is null ^ opposingComponent.PatternColour is null)
            {
                return false;
            }

            return component.PatternColour.IsSameColourAs(opposingComponent.PatternColour);
        }

        /// <summary>
        /// Builds a shift message for the given character if the given transformation were to be applied.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The shift message.</returns>
        [Pure, NotNull]
        public string BuildShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            return ReplaceTokensWithContent(transformation.ShiftMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a shift message for the given character if the given transformation were to be applied to both chiral
        /// components at the same time.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The uniform shift message.</returns>
        [Pure, NotNull]
        public string BuildUniformShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            if (transformation.UniformShiftMessage is null)
            {
                throw new InvalidOperationException("Missing uniform shift description.");
            }

            return ReplaceTokensWithContent(transformation.UniformShiftMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a grow message for the given character if the given transformation were to be applied.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The grow message.</returns>
        [Pure, NotNull]
        public string BuildGrowMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            return ReplaceTokensWithContent(transformation.GrowMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a grow message for the given character if the given transformation were to be applied to both chiral
        /// components at the same time.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="component">The component to build the message from.</param>
        /// <returns>The uniform grow message.</returns>
        [Pure, NotNull]
        public string BuildUniformGrowMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent component
        )
        {
            var transformation = component.Transformation;

            if (transformation.UniformGrowMessage is null)
            {
                throw new InvalidOperationException("Missing uniform grow description.");
            }

            return ReplaceTokensWithContent(transformation.UniformGrowMessage, appearanceConfiguration, component);
        }

        /// <summary>
        /// Builds a removal message for the given character if the given transformation were to be applied.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="bodypart">The bodypart to build the message from.</param>
        /// <returns>The removal message.</returns>
        [Pure, NotNull]
        public string BuildRemoveMessage([NotNull]Appearance appearanceConfiguration, Bodypart bodypart)
        {
            string removalText;
            switch (bodypart)
            {
                case Bodypart.Hair:
                {
                    removalText = _transformationText.Messages.Removal.Single.Hair.PickRandom();
                    break;
                }
                case Bodypart.Face:
                {
                    removalText = _transformationText.Messages.Removal.Single.Face.PickRandom();
                    break;
                }
                case Bodypart.Ear:
                {
                    removalText = _transformationText.Messages.Removal.Single.Ear.PickRandom();
                    break;
                }
                case Bodypart.Eye:
                {
                    removalText = _transformationText.Messages.Removal.Single.Eye.PickRandom();
                    break;
                }
                case Bodypart.Teeth:
                {
                    removalText = _transformationText.Messages.Removal.Single.Teeth.PickRandom();
                    break;
                }
                case Bodypart.Leg:
                {
                    removalText = _transformationText.Messages.Removal.Single.Leg.PickRandom();
                    break;
                }
                case Bodypart.Arm:
                {
                    removalText = _transformationText.Messages.Removal.Single.Arm.PickRandom();
                    break;
                }
                case Bodypart.Tail:
                {
                    removalText = _transformationText.Messages.Removal.Single.Tail.PickRandom();
                    break;
                }
                case Bodypart.Wing:
                {
                    removalText = _transformationText.Messages.Removal.Single.Wing.PickRandom();
                    break;
                }
                case Bodypart.Penis:
                {
                    removalText = _transformationText.Messages.Removal.Single.Penis.PickRandom();
                    break;
                }
                case Bodypart.Vagina:
                {
                    removalText = _transformationText.Messages.Removal.Single.Vagina.PickRandom();
                    break;
                }
                case Bodypart.Head:
                {
                    removalText = _transformationText.Messages.Removal.Single.Head.PickRandom();
                    break;
                }
                case Bodypart.Body:
                {
                    removalText = _transformationText.Messages.Removal.Single.Body.PickRandom();
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return ReplaceTokensWithContent(removalText, appearanceConfiguration, null);
        }

        /// <summary>
        /// Builds a removal message for the given character if the given transformation were to be applied.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="bodypart">The bodypart to build the message from.</param>
        /// <returns>The removal message.</returns>
        [Pure, NotNull]
        public string BuildUniformRemoveMessage([NotNull]Appearance appearanceConfiguration, Bodypart bodypart)
        {
            string removalText;
            switch (bodypart)
            {
                case Bodypart.Leg:
                {
                    removalText = _transformationText.Messages.Removal.Uniform.Legs.PickRandom();
                    break;
                }
                case Bodypart.Arm:
                {
                    removalText = _transformationText.Messages.Removal.Uniform.Arms.PickRandom();
                    break;
                }
                case Bodypart.Wing:
                {
                    removalText = _transformationText.Messages.Removal.Uniform.Wings.PickRandom();
                    break;
                }
                case Bodypart.Ear:
                {
                    removalText = _transformationText.Messages.Removal.Uniform.Ears.PickRandom();
                    break;
                }
                case Bodypart.Eye:
                {
                    removalText = _transformationText.Messages.Removal.Uniform.Eyes.PickRandom();
                    break;
                }
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }

            return ReplaceTokensWithContent(removalText, appearanceConfiguration, null);
        }

        /// <summary>
        /// Builds a pattern colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Shifting.Single.PatternColour.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Shifting.Uniform.PatternColour.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Shifting.Single.Pattern.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Shifting.Uniform.Pattern.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern addition message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternAddMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Adding.Single.Pattern.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern addition message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternAddMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Adding.Uniform.Pattern.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern removal message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildPatternRemoveMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Removal.Single.Pattern.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a pattern removal message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformPatternRemoveMessage
        (
            [NotNull]Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Removal.Uniform.Pattern.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a base colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Shifting.Single.Colour.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }

        /// <summary>
        /// Builds a base uniform colour shifting message for the given character and component.
        /// </summary>
        /// <param name="appearanceConfiguration">The appearance configuration to use as a base.</param>
        /// <param name="currentComponent">The current component.</param>
        /// <returns>The shifting message.</returns>
        [Pure, NotNull]
        public string BuildUniformColourShiftMessage
        (
            [NotNull] Appearance appearanceConfiguration,
            [NotNull] AppearanceComponent currentComponent
        )
        {
            var shiftMessage = _transformationText.Messages.Shifting.Uniform.Colour.PickRandom();
            return ReplaceTokensWithContent(shiftMessage, appearanceConfiguration, currentComponent);
        }
    }
}
