//
//  ReplacableTextToken.cs
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
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace DIGOS.Ambassador.Plugins.Transformations.Transformations
{
    /// <summary>
    /// Base implementation of replacable text tokens, allowing static initialization.
    /// </summary>
    /// <typeparam name="T">A class inheriting from this class.</typeparam>
    [PublicAPI]
    public abstract class ReplacableTextToken<T> : IReplaceableTextToken where T : ReplacableTextToken<T>
    {
        /// <inheritdoc />
        public int Start { get; set; }

        /// <inheritdoc />
        public int Length { get; set; }

        /// <inheritdoc />
        public abstract string GetText(Appearance appearance, AppearanceComponent component);

        /// <inheritdoc />
        public virtual Task<string> GetTextAsync(Appearance appearance, AppearanceComponent component)
        {
            return Task.Run(() => GetText(appearance, component));
        }

        /// <summary>
        /// Initializes the token with generic data coming from the text.
        /// </summary>
        /// <param name="data">Generic data.</param>
        /// <returns>An initialized instance of a token.</returns>
        [NotNull]
        protected abstract T Initialize([CanBeNull] string data);

        /// <summary>
        /// Creates a new, initialized token from the given start and end positions, along with generic data.
        /// </summary>
        /// <param name="start">The index in the original text where the token starts.</param>
        /// <param name="length">The length of the original token.</param>
        /// <param name="data">Generic data.</param>
        /// <param name="services">The application's service collection.</param>
        /// <returns>An initialized instance of a token.</returns>
        [NotNull]
        public static T CreateFrom(int start, int length, [CanBeNull] string data, [NotNull] IServiceProvider services)
        {
            var token = (ReplacableTextToken<T>)ActivatorUtilities.CreateInstance(services, typeof(T));
            token.Start = start;
            token.Length = length;

            return token.Initialize(data);
        }
    }
}
