//
//  KeyValuePairExtensions.cs
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

namespace DIGOS.Ambassador.Core.Extensions
{
    /// <summary>
    /// Extensions to the <see cref="KeyValuePair{TKey, TValue}"/> class.
    /// </summary>
    public static class KeyValuePairExtensions
    {
        /// <summary>
        /// Deconstructs a key-value pair into a value tuple.
        /// </summary>
        /// <param name="this">The pair.</param>
        /// <param name="key">Will be filled with the key.</param>
        /// <param name="value">Will be filled with the value.</param>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <typeparam name="TValue">The type of the value.</typeparam>
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> @this, out TKey key, out TValue value)
        {
            key = @this.Key;
            value = @this.Value;
        }
    }
}
