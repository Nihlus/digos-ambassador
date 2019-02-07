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
        [DescriptionPriority(9)]
        Hair,

        /// <summary>
        /// The face.
        /// </summary>
        [DescriptionPriority(10)]
        Face,

        /// <summary>
        /// An ear.
        /// </summary>
        [Chiral, DescriptionPriority(6)]
        Ear,

        /// <summary>
        /// An eye.
        /// </summary>
        [Chiral, DescriptionPriority(8)]
        Eye,

        /// <summary>
        /// The teeth.
        /// </summary>
        [DescriptionPriority(7)]
        Teeth,

        /// <summary>
        /// The main body.
        /// </summary>
        [DescriptionPriority(5)]
        Body,

        /// <summary>
        /// An arm.
        /// </summary>
        [Chiral, DescriptionPriority(3)]
        Arm,

        /// <summary>
        /// A leg.
        /// </summary>
        [Chiral, DescriptionPriority(2)]
        Leg,

        /// <summary>
        /// The tail.
        /// </summary>
        [DescriptionPriority(1)]
        Tail,

        /// <summary>
        /// A wing.
        /// </summary>
        [Chiral, DescriptionPriority(4)]
        Wing,

        /// <summary>
        /// The penis.
        /// </summary>
        [Gendered, DescriptionPriority(0)]
        Penis,

        /// <summary>
        /// The vagina.
        /// </summary>
        [Gendered, DescriptionPriority(0)]
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
