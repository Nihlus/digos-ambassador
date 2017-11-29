//
//  CompositeParts.cs
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
using JetBrains.Annotations;
using static DIGOS.Ambassador.Services.Bodypart;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Holds composite parts - that is, a body part that consists of a number of smaller parts, which should be
	/// collectively affected.
	/// </summary>
	public static class BodypartUtilities
	{
		private static IReadOnlyList<Bodypart> Composites { get; } = new[] { Bodypart.Head, Bodypart.Arms, Bodypart.Legs, Bodypart.Wings };

		private static IReadOnlyList<Bodypart> ChiralParts { get; } = new[] { LeftEar, RightEar, LeftEye, RightEye, LeftArm, RightArm, LeftLeg, RightLeg, LeftWing, RightWing };

		private static IDictionary<Bodypart, Bodypart> ChiralCounterparts { get; } = new Dictionary<Bodypart, Bodypart>
		{
			{ LeftEar, RightEar },
			{ RightEar, LeftEar },
			{ LeftEye, RightEye },
			{ RightEye, LeftEye },
			{ LeftArm, RightArm },
			{ RightArm, LeftArm },
			{ LeftLeg, RightLeg },
			{ RightLeg, LeftLeg },
			{ LeftWing, RightWing },
			{ RightWing, LeftWing }
		};

		private static IReadOnlyList<Bodypart> GenderNeutralParts { get; } = Enum
			.GetValues(typeof(Bodypart))
			.Cast<Bodypart>()
			.Except(new[] { Penis, Vagina })
			.ToArray();

		/// <summary>
		/// Gets the parts constituting the head.
		/// </summary>
		public static IReadOnlyList<Bodypart> Head { get; } = new[] { Face, LeftEar, RightEar, LeftEye, RightEye, Teeth };

		/// <summary>
		/// Gets the parts constituting the arms.
		/// </summary>
		public static IReadOnlyList<Bodypart> Arms { get; } = new[] { LeftArm, RightArm };

		/// <summary>
		/// Gets the parts constituting the legs.
		/// </summary>
		public static IReadOnlyList<Bodypart> Legs { get; } = new[] { LeftLeg, RightLeg };

		/// <summary>
		/// Gets the parts constituting the wings.
		/// </summary>
		public static IReadOnlyList<Bodypart> Wings { get; } = new[] { LeftWing, RightWing };

		/// <summary>
		/// Determines whether or not a given bodypart is considered gender-neutral.
		/// </summary>
		/// <param name="bodypart">The bodypart.</param>
		/// <returns>true if the part is gender-neutral; otherwise, false.</returns>
		public static bool IsGenderNeutral(Bodypart bodypart)
		{
			return GenderNeutralParts.Contains(bodypart);
		}

		/// <summary>
		/// Determines whether or not a given bodypart is a composite part.
		/// </summary>
		/// <param name="bodypart">The part to check.</param>
		/// <returns>true if the part is a composite part; otherwise, false.</returns>
		[Pure]
		public static bool IsCompositePart(Bodypart bodypart)
		{
			return Composites.Contains(bodypart);
		}

		/// <summary>
		/// Determines whether or not a given bodypart is part of a composite part.
		/// </summary>
		/// <param name="bodypart"></param>
		/// <returns></returns>
		[Pure]
		public static bool IsComposingPart(Bodypart bodypart)
		{
			return
				Head.Contains(bodypart) ||
				Arms.Contains(bodypart) ||
				Legs.Contains(bodypart) ||
				Wings.Contains(bodypart);
		}

		/// <summary>
		/// Determines whether or not the given part has a left- or right-handed counterpart.
		/// </summary>
		/// <param name="bodypart">The bodypart.</param>
		/// <returns>true if the part is a chiral part; otherwise, false.</returns>
		public static bool IsChiralPart(Bodypart bodypart)
		{
			return ChiralParts.Contains(bodypart);
		}

		/// <summary>
		/// Gets the chirally opposing part for the given chiral part.
		/// </summary>
		/// <param name="bodypart">The chiral part.</param>
		/// <returns>The chirally opposite part.</returns>
		/// <exception cref="ArgumentException">Thrown if the given bodypart is not a chiral part.</exception>
		public static Bodypart GetChiralPart(Bodypart bodypart)
		{
			if (!IsChiralPart(bodypart))
			{
				throw new ArgumentException("The bodypart is not a chiral part.", nameof(bodypart));
			}

			return ChiralCounterparts[bodypart];
		}

		/// <summary>
		/// Gets the bodyparts that a given part consists of.
		/// </summary>
		/// <param name="bodypart">The body part to decompose.</param>
		/// <returns>An iterator over the bodyparts in the given bodypart.</returns>
		[Pure]
		public static IEnumerable<Bodypart> GetBodyparts(Bodypart bodypart)
		{
			if (!IsCompositePart(bodypart))
			{
				yield return bodypart;
				yield break;
			}

			IEnumerable<Bodypart> parts;
			switch (bodypart)
			{
				case Bodypart.Head:
				{
					parts = Head;
					break;
				}
				case Bodypart.Arms:
				{
					parts = Arms;
					break;
				}
				case Bodypart.Legs:
				{
					parts = Legs;
					break;
				}
				case Bodypart.Wings:
				{
					parts = Wings;
					break;
				}
				default:
				{
					throw new ArgumentOutOfRangeException();
				}
			}

			foreach (var part in parts)
			{
				yield return part;
			}
		}
	}
}
