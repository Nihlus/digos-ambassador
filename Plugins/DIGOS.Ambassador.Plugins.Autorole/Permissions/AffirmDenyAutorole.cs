//
//  AffirmDenyAutorole.cs
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
using DIGOS.Ambassador.Plugins.Permissions;

namespace DIGOS.Ambassador.Plugins.Autorole.Permissions;

/// <summary>
/// Represents a permission that allows a user to affirm or deny autorole qualifications.
/// </summary>
public class AffirmDenyAutorole : Permission
{
    /// <inheritdoc />
    public override Guid UniqueIdentifier { get; } = new Guid("385AA18B-CBA2-4D5E-9B8A-077D18EE0225");

    /// <inheritdoc/>
    public override string FriendlyName { get; } = nameof(AffirmDenyAutorole);

    /// <inheritdoc/>
    public override string Description { get; } = "Allows you to affirm or deny autorole qualifications.";
}
