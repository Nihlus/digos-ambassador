//
//  TokenIdentifierAttribute.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Transformations
{
	/// <summary>
	/// Decorates a text token class with its in-text identifier.
	/// </summary>
	[MeansImplicitUse]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	public class TokenIdentifierAttribute : Attribute
	{
		/// <summary>
		/// Gets or sets the in-text identifier of the token.
		/// </summary>
		public string[] Identifiers { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="TokenIdentifierAttribute"/> class.
		/// </summary>
		/// <param name="identifiers">The identifiers.</param>
		public TokenIdentifierAttribute(params string[] identifiers)
		{
			this.Identifiers = identifiers;
		}
	}
}
