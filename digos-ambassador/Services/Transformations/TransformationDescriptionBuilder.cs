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

using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Database.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Service class for building user-visible descriptions of characters based on their appearances.
	/// </summary>
	public class TransformationDescriptionBuilder
	{
		/// <summary>
		/// Builds a complete visual description of the given character.
		/// </summary>
		/// <param name="character">The character to describe.</param>
		/// <returns>A visual description of the character.</returns>
		[Pure]
		public static string BuildVisualDescription(Character character)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Builds a shift message for the given character if the given transformation were to be applied.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="transformation">The transformation to build the message from.</param>
		/// <returns>The shift message.</returns>
		[Pure]
		public static string BuildShiftMessage([NotNull] Character character, [NotNull] Transformation transformation)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Builds a grow message for the given character if the given transformation were to be applied.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="transformation">The transformation to build the message from.</param>
		/// <returns>The grow message.</returns>
		[Pure]
		public static string BuildGrowMessage([NotNull] Character character, [NotNull] Transformation transformation)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Builds a removal message for the given character if the given transformation were to be applied.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="transformation">The transformation to build the message from.</param>
		/// <returns>The removal message.</returns>
		[Pure]
		public static string BuildRemoveMessage([NotNull] Character character, [NotNull] Transformation transformation)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Builds a pattern colour shifting message for the given character and component.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="originalColour">The original colour of the pattern.</param>
		/// <param name="currentComponent">The current component.</param>
		/// <returns>The shifting message.</returns>
		[Pure]
		public static string BuildPatternColourShiftMessage
		(
			[NotNull] Character character,
			[NotNull] Colour originalColour,
			[NotNull] AppearanceComponent currentComponent
		)
		{
			throw new System.NotImplementedException();
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
		public static string BuildPatternShiftMessage
		(
			[NotNull] Character character,
			[CanBeNull] Pattern? originalPattern,
			[NotNull] Colour originalColour,
			[NotNull] AppearanceComponent currentComponent
		)
		{
			throw new System.NotImplementedException();
		}

		/// <summary>
		/// Builds a base colour shifting message for the given character and component.
		/// </summary>
		/// <param name="character">The character to use as a base.</param>
		/// <param name="originalColour">The original colour of the pattern.</param>
		/// <param name="currentComponent">The current component.</param>
		/// <returns>The shifting message.</returns>
		[Pure]
		public static string BuildColourShiftMessage
		(
			[NotNull] Character character,
			[NotNull] Colour originalColour,
			[NotNull] AppearanceComponent currentComponent
		)
		{
			throw new System.NotImplementedException();
		}
	}
}
