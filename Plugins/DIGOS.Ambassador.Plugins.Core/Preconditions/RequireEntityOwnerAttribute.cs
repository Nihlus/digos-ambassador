//
//  RequireEntityOwnerAttribute.cs
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
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using Discord.Commands;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Core.Preconditions
{
    /// <summary>
    /// Acts as a precondition for owned entities, limiting their use to their owners.
    /// </summary>
    [PublicAPI]
    public class RequireEntityOwnerAttribute : ParameterPreconditionAttribute
    {
        /// <inheritdoc />
        [NotNull]
        public override Task<PreconditionResult> CheckPermissionsAsync
        (
            ICommandContext context,
            ParameterInfo parameter,
            object value,
            IServiceProvider services
        )
        {
            if (!(value is IOwnedNamedEntity entity))
            {
                return Task.FromResult(PreconditionResult.FromError("The value isn't an owned entity."));
            }

            if (entity.IsOwner(context.User))
            {
                return Task.FromResult(PreconditionResult.FromSuccess());
            }

            return Task.FromResult(PreconditionResult.FromError("You don't have permission to do that."));
        }
    }
}
