//
//  PlaceholderDataAttribute.cs
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

namespace DIGOS.Ambassador.Doc.Abstractions
{
    /// <summary>
    /// Tags an assembly with placeholder data for a given data type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class PlaceholderDataAttribute : Attribute
    {
        /// <summary>
        /// Gets the data type that the placeholder data is for.
        /// </summary>
            public Type DataType { get; }

        /// <summary>
        /// Gets the placeholder data.
        /// </summary>
            public string[] Placeholders { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderDataAttribute"/> class.
        /// </summary>
        /// <param name="dataType">The data type the placeholder data is for.</param>
        /// <param name="placeholders">The placeholder data.</param>
            public PlaceholderDataAttribute(Type dataType, params string[] placeholders)
        {
            this.DataType = dataType;
            this.Placeholders = placeholders;
        }
    }
}
