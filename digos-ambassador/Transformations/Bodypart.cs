//
//  Bodypart.cs
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

using DIGOS.Ambassador.Attributes;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// Represents a single transformable body part.
	/// </summary>
	public enum Bodypart
	{
		/// <summary>
		/// The head hair.
		/// </summary>
		Hair,

		/// <summary>
		/// The face.
		/// </summary>
		Face,

		/// <summary>
		/// An ear.
		/// </summary>
		[Chiral]
		Ear,

		/// <summary>
		/// An eye.
		/// </summary>
		[Chiral]
		Eye,

		/// <summary>
		/// The teeth.
		/// </summary>
		Teeth,

		/// <summary>
		/// The main body.
		/// </summary>
		Body,

		/// <summary>
		/// An arm.
		/// </summary>
		[Chiral]
		Arm,

		/// <summary>
		/// A leg.
		/// </summary>
		[Chiral]
		Leg,

		/// <summary>
		/// The tail.
		/// </summary>
		Tail,

		/// <summary>
		/// A wing.
		/// </summary>
		[Chiral]
		Wing,

		/// <summary>
		/// The penis.
		/// </summary>
		[Gendered]
		Penis,

		/// <summary>
		/// The vagina.
		/// </summary>
		[Gendered]
		Vagina,

		/// <summary>
		/// The eyes.
		/// </summary>
		[Composite(Eye)]
		Eyes,

		/// <summary>
		/// The head, composed of the face, the ears, the teeth, and the eyes.
		/// </summary>
		[Composite(Face, Ear, Teeth, Eye)]
		Head,

		/// <summary>
		/// The arms, composed of the left and right arms.
		/// </summary>
		[Composite(Arm)]
		Arms,

		/// <summary>
		/// The legs, composed of the left and right legs.
		/// </summary>
		[Composite(Leg)]
		Legs,

		/// <summary>
		/// The wings, composed of the left and right wings.
		/// </summary>
		[Composite(Wing)]
		Wings,
	}
}
