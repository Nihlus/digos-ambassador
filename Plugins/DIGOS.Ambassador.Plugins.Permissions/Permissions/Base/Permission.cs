//
//  Permission.cs
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
using DIGOS.Ambassador.Plugins.Permissions.Model;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions;

/// <summary>
/// Serves as a base class for permission types.
/// </summary>
[PublicAPI]
public abstract class Permission : IPermission
{
    /// <inheritdoc />
    public abstract Guid UniqueIdentifier { get; }

    /// <inheritdoc />
    public abstract string FriendlyName { get; }

    /// <inheritdoc />
    public abstract string Description { get; }

    /// <inheritdoc />
    public virtual bool IsGrantedByDefaultToSelf => false;

    /// <inheritdoc />
    public virtual bool IsGrantedByDefaultToOthers => false;

    /// <inheritdoc/>
    public bool IsGrantedByDefaultTo(PermissionTarget target)
    {
        return target switch
        {
            PermissionTarget.Self => this.IsGrantedByDefaultToSelf,
            PermissionTarget.Other => this.IsGrantedByDefaultToOthers,
            PermissionTarget.All => this.IsGrantedByDefaultToSelf && this.IsGrantedByDefaultToOthers,
            _ => throw new ArgumentOutOfRangeException(nameof(target), target, null)
        };
    }
}
