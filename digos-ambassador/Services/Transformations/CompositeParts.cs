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

using System.Collections.Generic;
using static DIGOS.Ambassador.Services.Transformations.Bodypart;

namespace DIGOS.Ambassador.Services.Transformations
{
	/// <summary>
	/// Holds composite parts - that is, a body part that consists of a number of smaller parts, which should be
	/// collectively affected.
	/// </summary>
	public static class CompositeParts
	{
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
	}
}
