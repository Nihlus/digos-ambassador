﻿//
//  EnumExtensions.cs
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace DIGOS.Ambassador.Plugins.Transformations.Extensions
{
    /// <summary>
    /// Extensions for the <see cref="Enum"/> class.
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// Attempts to get an instance of a custom attribute from the given enum value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="customAttribute">The attribute.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>true if the attribute was successfully retrieved; otherwise, false.</returns>
        public static bool TryGetCustomAttribute<T>
        (
            this Enum value,
            [NotNullWhen(true)] out T? customAttribute
        ) where T : Attribute
        {
            customAttribute = value.GetCustomAttribute<T>();
            return customAttribute is not null;
        }

        /// <summary>
        /// Gets a custom attribute of type <typeparamref name="T"/> from the given enum value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <typeparam name="T">The attribute type.</typeparam>
        /// <returns>The attribute.</returns>
        public static T? GetCustomAttribute<T>(this Enum value) where T : Attribute
        {
            var enumType = value.GetType();
            var name = Enum.GetName(enumType, value);
            return name is null
                ? null
                : enumType.GetField(name)?.GetCustomAttributes(false).OfType<T>().SingleOrDefault();
        }

        /// <summary>
        /// Determines whether or not the given enum value has a custom attribute of type <typeparamref name="T"/>.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <typeparam name="T">The type.</typeparam>
        /// <returns>true if the value has an attribute; otherwise, false.</returns>
        public static bool HasCustomAttribute<T>(this Enum value) where T : Attribute
        {
            return value.GetCustomAttribute<T>() is not null;
        }
    }
}
