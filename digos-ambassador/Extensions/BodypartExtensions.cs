//
//  BodypartExtensions.cs
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
using DIGOS.Ambassador.Attributes;
using DIGOS.Ambassador.Transformations;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Extensions
{
	/// <summary>
	/// Extensions for the <see cref="Bodypart"/> enum.
	/// </summary>
	public static class BodypartExtensions
	{
		/// <summary>
		/// Determines whether or not a given bodypart is considered gender-neutral.
		/// </summary>
		/// <param name="this">The bodypart.</param>
		/// <returns>true if the part is gender-neutral; otherwise, false.</returns>
		[Pure]
		public static bool IsGenderNeutral(this Bodypart @this)
		{
			return !@this.HasCustomAttribute<GenderedAttribute>();
		}

		/// <summary>
		/// Determines whether or not a given bodypart is a composite part.
		/// </summary>
		/// <param name="this">The part to check.</param>
		/// <returns>true if the part is a composite part; otherwise, false.</returns>
		[Pure]
		public static bool IsComposite(this Bodypart @this)
		{
			return @this.HasCustomAttribute<CompositeAttribute>();
		}

		/// <summary>
		/// Determines whether or not a given bodypart is part of a composite part.
		/// </summary>
		/// <param name="this">The bodypart to check.</param>
		/// <returns>true if the bodypart is a composing part; otherwise, false.</returns>
		[Pure]
		public static bool IsComposingPart(this Bodypart @this)
		{
			var compositeParts = Enum.GetValues(typeof(Bodypart))
			.Cast<Bodypart>()
			.Where
			(
				b =>
					b.HasCustomAttribute<CompositeAttribute>()
			)
			.SelectMany
			(
				b =>
					b.GetCustomAttribute<CompositeAttribute>().ComposingParts
			);

			return compositeParts.Contains(@this);
		}

		/// <summary>
		/// Determines whether or not the given part has a left- or right-handed counterpart.
		/// </summary>
		/// <param name="this">The bodypart.</param>
		/// <returns>true if the part is a chiral part; otherwise, false.</returns>
		public static bool IsChiral(this Bodypart @this)
		{
			return @this.HasCustomAttribute<ChiralAttribute>();
		}

		/// <summary>
		/// Gets the bodyparts that a given part consists of.
		/// </summary>
		/// <param name="this">The body part to decompose.</param>
		/// <returns>An iterator over the bodyparts in the given bodypart.</returns>
		[Pure]
		public static IEnumerable<Bodypart> GetComposingParts(this Bodypart @this)
		{
			if (!IsComposite(@this))
			{
				yield return @this;
				yield break;
			}

			foreach (var part in @this.GetCustomAttribute<CompositeAttribute>().ComposingParts)
			{
				yield return part;
			}
		}
	}
}
