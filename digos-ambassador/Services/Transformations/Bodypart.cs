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

namespace DIGOS.Ambassador.Services
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
		/// The left ear.
		/// </summary>
		LeftEar,

		/// <summary>
		/// The right ear.
		/// </summary>
		RightEar,

		/// <summary>
		/// The left eye.
		/// </summary>
		LeftEye,

		/// <summary>
		/// The right eye.
		/// </summary>
		RightEye,

		/// <summary>
		/// The teeth.
		/// </summary>
		Teeth,

		/// <summary>
		/// The upper body.
		/// </summary>
		UpperBody,

		/// <summary>
		/// The left arm.
		/// </summary>
		LeftArm,

		/// <summary>
		/// The right arm.
		/// </summary>
		RightArm,

		/// <summary>
		/// The lower body.
		/// </summary>
		LowerBody,

		/// <summary>
		/// The left leg.
		/// </summary>
		LeftLeg,

		/// <summary>
		/// The right leg.
		/// </summary>
		RightLeg,

		/// <summary>
		/// The tail.
		/// </summary>
		Tail,

		/// <summary>
		/// The left wing.
		/// </summary>
		LeftWing,

		/// <summary>
		/// The right wing.
		/// </summary>
		RightWing,

		/// <summary>
		/// The penis.
		/// </summary>
		Penis,

		/// <summary>
		/// The vagina.
		/// </summary>
		Vagina,

		/// <summary>
		/// The head, composed of the face, the ears, the teeth, and the eyes.
		/// </summary>
		Head,

		/// <summary>
		/// The arms, composed of the left and right arms.
		/// </summary>
		Arms,

		/// <summary>
		/// The body, composed of the upper and lower body.
		/// </summary>
		Body,

		/// <summary>
		/// The legs, composed of the left and right legs.
		/// </summary>
		Legs,

		/// <summary>
		/// The wings, composed of the left and right wings.
		/// </summary>
		Wings,
	}
}
