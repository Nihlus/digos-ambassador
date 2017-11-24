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
	public static class CompositeParts
	{
		private static IReadOnlyList<Bodypart> Composites { get; } = new[] { Bodypart.Head, Bodypart.Arms, Bodypart.Body, Bodypart.Legs, Bodypart.Wings };

		/// <summary>
		/// Gets the parts constituting the head.
		/// </summary>
		public static IReadOnlyList<Bodypart> Head { get; } = new[] { Face, LeftEar, RightEar, LeftEye, RightEye, Teeth };

		/// <summary>
		/// Gets the parts constituting the arms.
		/// </summary>
		public static IReadOnlyList<Bodypart> Arms { get; } = new[] { LeftArm, RightArm };

		/// <summary>
		/// Gets the parts constituting the body.
		/// </summary>
		public static IReadOnlyList<Bodypart> Body { get; } = new[] { UpperBody, LowerBody };

		/// <summary>
		/// Gets the parts constituting the legs.
		/// </summary>
		public static IReadOnlyList<Bodypart> Legs { get; } = new[] { LeftLeg, RightLeg };

		/// <summary>
		/// Gets the parts constituting the wings.
		/// </summary>
		public static IReadOnlyList<Bodypart> Wings { get; } = new[] { LeftWing, RightWing };

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
				case Bodypart.Body:
				{
					parts = Body;
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
