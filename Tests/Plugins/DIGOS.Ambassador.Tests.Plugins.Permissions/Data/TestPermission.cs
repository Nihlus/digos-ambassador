//
//  TestPermission.cs
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

namespace DIGOS.Ambassador.Tests.Plugins.Permissions.Data;

/// <summary>
/// Defines a simple test permission.
/// </summary>
public class TestPermission : Permission
{
    /// <inheritdoc />
    public override Guid UniqueIdentifier { get; } = new Guid("522DD3BD-BBEA-4C5F-AB95-2A04DBCB606B");

    /// <inheritdoc />
    public override string FriendlyName => nameof(TestPermission);

    /// <inheritdoc />
    public override string Description => "A test permission.";
}
