//
//  ModifyEntityResult.cs
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
    /// Encapsulates the result of an attempt to add or edit an entity.
    /// </summary>
    public class ModifyEntityResult : ResultBase<ModifyEntityResult>
    {
        /// <summary>
        /// Gets the action that was taken on the entity.
        /// </summary>
        public ModifyEntityAction? ActionTaken { get; }

        /// <summary>
        /// Gets a value indicating whether or not any entity was modified.
        /// </summary>
        public bool WasModified => this.ActionTaken.HasValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModifyEntityResult"/> class.
        /// </summary>
        /// <param name="actionTaken">The action that was taken on the entity.</param>
        private ModifyEntityResult([CanBeNull] ModifyEntityAction? actionTaken)
        {
            this.ActionTaken = actionTaken;
        }

        /// <inheritdoc cref="ResultBase{TResultType}(CommandError?,string,Exception)"/>
        [UsedImplicitly]
        private ModifyEntityResult([CanBeNull] CommandError? error, [CanBeNull] string errorReason, [CanBeNull] Exception exception = null)
            : base(error, errorReason, exception)
        {
        }

        /// <summary>
        /// Creates a new successful result.
        /// </summary>
        /// <param name="actionTaken">The action that was taken on the entity.</param>
        /// <returns>A successful result.</returns>
        [Pure]
        public static ModifyEntityResult FromSuccess(ModifyEntityAction actionTaken = ModifyEntityAction.Edited)
        {
            return new ModifyEntityResult(actionTaken);
        }
    }
}
