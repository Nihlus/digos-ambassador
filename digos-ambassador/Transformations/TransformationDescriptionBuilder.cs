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
using System.Linq;
using System.Text;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Services;
using Humanizer;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// Service class for building user-visible descriptions of characters based on their appearances.
	/// </summary>
	public class TransformationDescriptionBuilder
	{
		private readonly TransformationTextTokenizer Tokenizer;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformationDescriptionBuilder"/> class.
		/// </summary>
		/// <param name="services">The available services.</param>
		public TransformationDescriptionBuilder(IServiceProvider services)
		{
			this.Tokenizer = new TransformationTextTokenizer(services);

			this.Tokenizer.DiscoverAvailableTokens();
		}

		/// <summary>
		/// Replaces tokens in a piece of text with their respective contents.
		/// </summary>
		/// <param name="text">The text to replace in.</param>
		/// <param name="character">The character for which the text should be valid.</param>
		/// <param name="transformation">The transformation that the text belongs to.</param>
		/// <returns>A string with no tokens in it.</returns>
		public string ReplaceTokensWithContent
		(
			[NotNull] string text,
			[NotNull] Character character,
			[NotNull] Transformation transformation
		)
		{
			var tokens = this.Tokenizer.GetTokens(text);
			var tokenContentMap = tokens.ToDictionary(token => token, token => token.GetText(character, transformation));

			int relativeOffset = 0;
			var sb = new StringBuilder(text);

			foreach (var (token, content) in tokenContentMap)
			{
				sb.Remove(token.Start + relativeOffset, token.Length);
				sb.Insert(token.Start + relativeOffset, content);

				relativeOffset += content.Length - token.Length;
			}

			var result = string.Join(". ", sb.ToString().Split('.').Select(s => s.Trim().Transform(To.SentenceCase))).Trim();
			return result;
		}

		/// <summary>
		/// Builds a complete visual description of the given character.
		/// </summary>
		/// <param name="character">The character to describe.</param>
		/// <returns>A visual description of the character.</returns>
		[Pure]
		public string BuildVisualDescription(Character character)
		{
			var sb = new StringBuilder();
			sb.Append("{@target} is a {@gender} {@species.}");

			int componentCount = 0;
			foreach (var component in character.CurrentAppearance.Components)
			{
				++componentCount;
				var csb = new StringBuilder();

				var transformation = component.Transformation;
				var bodypart = component.Bodypart;
				if (BodypartUtilities.IsChiralPart(component.Bodypart))
				{
					var sameSpecies = AreChiralPartsTheSameSpecies(character, bodypart);
					csb.Append
					(
						sameSpecies
							? transformation.UniformDescription
							: transformation.SingleDescription
					);
				}
				else
				{
					csb.Append(transformation.SingleDescription);
				}

				if (component.Pattern.HasValue)
				{
					csb.Append("A {@pattern}, {@colour|pattern} pattern covers it.");
				}

				var tokenizedDesc = csb.ToString();
				var componentDesc = ReplaceTokensWithContent(tokenizedDesc, character, transformation);

				sb.Append(componentDesc);

				// Break the description into paragraphs every third component
				if (componentCount % 3 == 0)
				{
					sb.Append("\n\n");
				}
			}

			return sb.ToString().Trim();
		}

		/// <summary>
		/// Determines if the chiral parts on the character are the same species.
		/// </summary>
		/// <param name="character">The character.</param>
		/// <param name="bodypart">The chiral bodypart.</param>
		/// <returns>true if the parts are the same species; otherwise, false.</returns>
		private bool AreChiralPartsTheSameSpecies(Character character, Bodypart bodypart)
		{
			var chiralComponent = character.GetBodypart(bodypart);
			var opposingComponent = character.GetBodypart(BodypartUtilities.GetChiralPart(bodypart));

			return chiralComponent.Transformation.Species.Name.Equals(opposingComponent.Transformation.Species.Name);
		}

		/// <summary>
		/// Builds a shift message for the given character if the given transformation were to be applied.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="transformation">The transformation to build the message from.</param>
		/// <returns>The shift message.</returns>
		[Pure]
		public string BuildShiftMessage([NotNull] Character character, [NotNull] Transformation transformation)
		{
			return ReplaceTokensWithContent(transformation.ShiftMessage, character, transformation);
		}

		/// <summary>
		/// Builds a grow message for the given character if the given transformation were to be applied.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="transformation">The transformation to build the message from.</param>
		/// <returns>The grow message.</returns>
		[Pure]
		public string BuildGrowMessage([NotNull] Character character, [NotNull] Transformation transformation)
		{
			return ReplaceTokensWithContent(transformation.GrowMessage, character, transformation);
		}

		/// <summary>
		/// Builds a removal message for the given character if the given transformation were to be applied.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="transformation">The transformation to build the message from.</param>
		/// <returns>The removal message.</returns>
		[Pure]
		public string BuildRemoveMessage([NotNull] Character character, [NotNull] Transformation transformation)
		{
			string removalText;
			switch (transformation.Part)
			{
				case Bodypart.Hair:
				{
					removalText = $"{{@target}}'s hair becomes dull and colourless. Strand by strand, tuft by tuft, it falls out, leaving an empty scalp.";
					break;
				}
				case Bodypart.Face:
				{
					removalText = $"{{@target}}'s face begins to warp strangely. Slowly, their features smooth and vanish, leaving a blank surface.";
					break;
				}
				case Bodypart.LeftEar:
				case Bodypart.RightEar:
				{
					removalText = $"{{@target}}'s {transformation.Part.Humanize()} shrivels and vanishes.";
					break;
				}
				case Bodypart.LeftEye:
				case Bodypart.RightEye:
				{
					removalText = $"{{@target}}'s {transformation.Part.Humanize()} deflates as their eye socket closes, leaving nothing behind.";
					break;
				}
				case Bodypart.Teeth:
				{
					removalText = $"With a strange popping sound, {{@target}}'s teeth retract and disappear.";
					break;
				}
				case Bodypart.LeftLeg:
				case Bodypart.RightLeg:
				case Bodypart.LeftArm:
				case Bodypart.RightArm:
				{
					removalText = $"{{@target}}'s {transformation.Part.Humanize()} shrivels and retracts, vanishing.";
					break;
				}
				case Bodypart.Tail:
				{
					removalText = $"{{@target}}'s tail flicks and thrashes for a moment, before it thins out and disappears into nothing.";
					break;
				}
				case Bodypart.LeftWing:
				case Bodypart.RightWing:
				{
					removalText = $"{{@target}}'s {transformation.Part.Humanize()} stiffens and shudders, before losing cohesion and disappearing into their body.";
					break;
				}
				case Bodypart.Penis:
				{
					removalText = $"{{@target}}'s shaft twitches and shudders, as it begins to shrink and retract. In mere moments, it's gone, leaving nothing.";
					break;
				}
				case Bodypart.Vagina:
				{
					removalText = $"{{@target}}'s slit contracts and twitches. A strange sensation rushes through {{@f|them}} as the opening zips up and fills out, leaving nothing.";
					break;
				}
				case Bodypart.Head:
				{
					removalText = $"{{@target}}'s head warps strangely before it deflates like a balloon, disappearing.";
					break;
				}
				case Bodypart.Legs:
				case Bodypart.Arms:
				{
					removalText = $"{{@target}}'s {transformation.Part.Humanize()} shrivel and retract, vanishing.";
					break;
				}
				case Bodypart.Body:
				{
					removalText = $"{{@target}}'s torso crumples into itself as their main body collapses, shifting and vanishing.";
					break;
				}
				case Bodypart.Wings:
				{
					removalText = $"{{@target}}'s {transformation.Part.Humanize()} stiffen and shudder, before losing cohesion and disappearing into their body.";
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}

			return ReplaceTokensWithContent(removalText, character, transformation);
		}

		/// <summary>
		/// Builds a pattern colour shifting message for the given character and component.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="originalColour">The original colour of the pattern.</param>
		/// <param name="currentComponent">The current component.</param>
		/// <returns>The shifting message.</returns>
		[Pure]
		public string BuildPatternColourShiftMessage
		(
			[NotNull] Character character,
			[NotNull] Colour originalColour,
			[NotNull] AppearanceComponent currentComponent
		)
		{
			string shiftMessage =
				$"{{@target}}'s {currentComponent.Bodypart.Humanize()} morphs, as" +
				$" {{@f|their}} {{@pattern}} {originalColour} hues turn into {currentComponent.PatternColour}.";

			return ReplaceTokensWithContent(shiftMessage, character, currentComponent.Transformation);
		}

		/// <summary>
		/// Builds a pattern shifting message for the given character and component.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="originalPattern">The original pattern.</param>
		/// <param name="originalColour">The original colour of the pattern.</param>
		/// <param name="currentComponent">The current component.</param>
		/// <returns>The shifting message.</returns>
		[Pure]
		public string BuildPatternShiftMessage
		(
			[NotNull] Character character,
			[CanBeNull] Pattern? originalPattern,
			[NotNull] Colour originalColour,
			[NotNull] AppearanceComponent currentComponent
		)
		{
			string shiftMessage =
				$"The surface of {{@target}}'s {currentComponent.Bodypart.Humanize()} morphs, as" +
				$" {{@colour}} {{@pattern}} patterns spread across it" +
				$"{(originalPattern.HasValue ? $", replacing their {originalColour} {originalPattern.Humanize().Pluralize()}" : ".")}.";

			return ReplaceTokensWithContent(shiftMessage, character, currentComponent.Transformation);
		}

		/// <summary>
		/// Builds a base colour shifting message for the given character and component.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="originalColour">The original colour of the pattern.</param>
		/// <param name="currentComponent">The current component.</param>
		/// <returns>The shifting message.</returns>
		[Pure]
		public string BuildColourShiftMessage
		(
			[NotNull] Character character,
			[NotNull] Colour originalColour,
			[NotNull] AppearanceComponent currentComponent
		)
		{
			string shiftMessage =
				$"{{@target}}'s {currentComponent.Bodypart.Humanize()} morphs, as" +
				$" {{@f|their}} {originalColour} hues turn into {currentComponent.PatternColour}.";

			return ReplaceTokensWithContent(shiftMessage, character, currentComponent.Transformation);
		}
	}
}
