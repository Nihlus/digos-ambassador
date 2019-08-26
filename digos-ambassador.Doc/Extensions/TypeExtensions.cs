//
//  TypeExtensions.cs
//
//  Author:
//      Jarl Gullberg <jarl.gullberg@gmail.com>
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
using JetBrains.Annotations;
using Mono.Cecil;

namespace DIGOS.Ambassador.Doc.Extensions
{
    /// <summary>
    /// Holds extension methods for types.
    /// </summary>
    public static class TypeExtensions
    {
        private static readonly Dictionary<Type, string> TypeAliases = new Dictionary<Type, string>
        {
            { typeof(string), "string" },
            { typeof(int), "int" },
            { typeof(byte), "byte" },
            { typeof(sbyte), "sbyte" },
            { typeof(short), "short" },
            { typeof(ushort), "ushort" },
            { typeof(long), "long" },
            { typeof(uint), "uint" },
            { typeof(ulong), "ulong" },
            { typeof(float), "float" },
            { typeof(double), "double" },
            { typeof(decimal), "decimal" },
            { typeof(object), "object" },
            { typeof(bool), "bool" },
            { typeof(char), "char" }
        };

        /// <summary>
        /// Humanizes a type, returning its type alias or full name.
        /// </summary>
        /// <param name="this">The type to humanize.</param>
        /// <returns>A humanized string.</returns>
        [Pure]
        public static string Humanize([NotNull] this TypeReference @this)
        {
            if (@this.IsPrimitive || @this.Name == nameof(String))
            {
                var resolvedType = Type.GetType($"{@this.FullName}");

                if (resolvedType is null)
                {
                    throw new InvalidOperationException();
                }

                return TypeAliases.ContainsKey(resolvedType) ? TypeAliases[resolvedType] : @this.Name;
            }

            return @this.Name;
        }
    }
}
