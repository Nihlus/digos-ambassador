//
//  EditRoleplay.cs
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
using DIGOS.Ambassador.Plugins.Permissions;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Permissions;

/// <summary>
/// Represents a permission that allows a user to edit roleplays.
/// </summary>
public sealed class EditRoleplay : Permission
{
    /// <inheritdoc />
    public override Guid UniqueIdentifier { get; } = new("83B12FE6-A71B-4449-96B6-A069A2987A27");

    /// <inheritdoc />
    public override string FriendlyName => nameof(EditRoleplay);

    /// <inheritdoc />
    public override string Description => "Allows you to edit roleplays.";

    /// <inheritdoc />
    public override bool IsGrantedByDefaultToSelf => true;
}
