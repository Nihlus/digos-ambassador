//
//  GrantPermission.cs
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
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Permissions;

/// <summary>
/// Represents a permission that allows a user to grant permissions.
/// </summary>
[PublicAPI]
public sealed class GrantPermission : Permission
{
    /// <inheritdoc />
    public override Guid UniqueIdentifier { get; } = new("E87BCAC2-094B-4576-B554-FC5C1F943401");

    /// <inheritdoc />
    public override string FriendlyName => nameof(GrantPermission);

    /// <inheritdoc />
    public override string Description => "Allows you to grant permissions.";
}
