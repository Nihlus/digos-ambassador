//
//  RetrieveEntityResult.cs
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
using DIGOS.Ambassador.Services.Base;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Services
{
    /// <summary>
    /// Represents an attempt to retrieve a roleplay from the database.
    /// </summary>
    /// <typeparam name="T">The entity type to encapsulate.</typeparam>
    public class RetrieveEntityResult<T> : ResultBase<RetrieveEntityResult<T>> where T : class
    {
        /// <summary>
        /// Holds the actual entity value.
        /// </summary>
        [CanBeNull]
        private readonly T _entity;

        /// <summary>
        /// Gets the entity that was retrieved.
        /// </summary>
        [NotNull]
        public T Entity
        {
            get
            {
                if (!this.IsSuccess || _entity is null)
                {
                    throw new InvalidOperationException("The result does not contain a valid value.");
                }

                return _entity;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RetrieveEntityResult{T}"/> class.
        /// </summary>
        /// <param name="entity">The entity.</param>
        private RetrieveEntityResult([CanBeNull] T entity)
        {
            _entity = entity;
        }

        /// <inheritdoc cref="ResultBase{TResultType}(CommandError?,string,Exception)"/>
        [UsedImplicitly]
        private RetrieveEntityResult([CanBeNull] CommandError? error, [CanBeNull] string errorReason, [CanBeNull] Exception exception = null)
            : base(error, errorReason, exception)
        {
        }

        /// <summary>
        /// Creates a new successful result.
        /// </summary>
        /// <param name="entity">The roleplay that was retrieved.</param>
        /// <returns>A successful result.</returns>
        [Pure]
        public static RetrieveEntityResult<T> FromSuccess([NotNull] T entity)
        {
            return new RetrieveEntityResult<T>(entity);
        }
    }
}
