//
//  PlaceholderData.cs
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
using System.Reflection;
using Mono.Cecil;

namespace DIGOS.Ambassador.Doc.Data
{
    /// <summary>
    /// Provides access to registered placeholder data.
    /// </summary>
    public class PlaceholderData
    {
        private readonly Dictionary<string, IReadOnlyCollection<string>> _placeholderCollections;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaceholderData"/> class.
        /// </summary>
        public PlaceholderData()
        {
            _placeholderCollections = new Dictionary<string, IReadOnlyCollection<string>>();

            RegisterBuiltinPlaceholders();
        }

        /// <summary>
        /// Registers placeholder data for builtin (CLR) types.
        /// </summary>
        private void RegisterBuiltinPlaceholders()
        {
            var thisModule = ModuleDefinition.ReadModule(Assembly.GetExecutingAssembly().Location);

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(bool)).Resolve(),
                new[] { "true", "false" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(byte)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(sbyte)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(short)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(ushort)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(int)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(uint)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(long)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(ulong)).Resolve(),
                new[] { "10", "20", "30", "40" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(char)).Resolve(),
                new[] { "a", "b", "c", "d" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(DateTimeOffset)).Resolve(),
                new[] { "5m", "1h", "1d", "24h" }
            );

            RegisterPlaceholderData
            (
                thisModule.ImportReference(typeof(TimeSpan)).Resolve(),
                new[] { "5m", "1h", "1d", "24h" }
            );
        }

        /// <summary>
        /// Registers placeholder data for the given type reference.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="placeholders">The placeholders.</param>
        public void RegisterPlaceholderData(TypeDefinition type, IReadOnlyCollection<string> placeholders)
        {
            if (_placeholderCollections.ContainsKey(type.FullName))
            {
                _placeholderCollections[type.FullName] = placeholders;
                return;
            }

            _placeholderCollections.Add(type.FullName, placeholders);
        }

        /// <summary>
        /// Determines whether the data repository has placeholder data for the given type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>true if the type has available placeholders; otherwise, false.</returns>
        public bool HasPlaceholders(TypeDefinition type)
        {
            return _placeholderCollections.ContainsKey(type.FullName);
        }

        /// <summary>
        /// Gets a number of placeholder items from the data registry, fitting the given type.
        /// </summary>
        /// <param name="dataType">The data type to get placeholders for.</param>
        /// <param name="count">The number of placeholders to get.</param>
        /// <returns>The placeholder items.</returns>
        public IEnumerable<string> GetPlaceholders(TypeDefinition dataType, int count = 1)
        {
            if (!_placeholderCollections.TryGetValue(dataType.FullName, out var placeholders))
            {
                yield break;
            }

            var yielded = 0;

            while (yielded < count)
            {
                foreach (var placeholder in placeholders)
                {
                    yield return placeholder;
                    ++yielded;

                    if (yielded >= count)
                    {
                        break;
                    }
                }
            }
        }
    }
}
