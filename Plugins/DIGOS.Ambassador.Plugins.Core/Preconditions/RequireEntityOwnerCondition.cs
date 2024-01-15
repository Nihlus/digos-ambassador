//
//  RequireEntityOwnerCondition.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
using DIGOS.Ambassador.Plugins.Core.Model.Entity;
using Remora.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Core.Preconditions;

/// <summary>
/// Acts as a precondition for owned entities, limiting their use to their owners.
/// </summary>
/// <typeparam name="TEntity">The entity type.</typeparam>
public class RequireEntityOwnerCondition<TEntity> : ICondition<RequireEntityOwnerAttribute, TEntity>
    where TEntity : IOwnedNamedEntity
{
    private readonly IOperationContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequireEntityOwnerCondition{TEntity}"/> class.
    /// </summary>
    /// <param name="context">The command context.</param>
    public RequireEntityOwnerCondition(IOperationContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public ValueTask<Result> CheckAsync
    (
        RequireEntityOwnerAttribute attribute,
        TEntity data,
        CancellationToken ct = default
    )
    {
        if (!_context.TryGetUserID(out var userID))
        {
            throw new InvalidOperationException();
        }

        return data.IsOwner(userID)
            ? new ValueTask<Result>(Result.FromSuccess())
            : new ValueTask<Result>(new UserError("You don't have permission to do that."));
    }
}
