//
//  RequirePermissionAttribute.cs
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
using DIGOS.Ambassador.Plugins.Permissions.Model;
using JetBrains.Annotations;
using Remora.Commands.Conditions;

namespace DIGOS.Ambassador.Plugins.Permissions.Conditions;

/// <summary>
/// This attribute can be attached to Discord.Net.Commands module commands to restrict them to certain predefined
/// permissions.
/// </summary>
[PublicAPI]
[AttributeUsage(AttributeTargets.Method)]
public class RequirePermissionAttribute : ConditionAttribute
{
    /// <summary>
    /// Gets the permission type.
    /// </summary>
    public Type Type { get; }

    /// <summary>
    /// Gets the permission target.
    /// </summary>
    public PermissionTarget Target { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequirePermissionAttribute"/> class.
    /// </summary>
    /// <param name="type">The required permission.</param>
    /// <param name="target">The required target scope.</param>
    public RequirePermissionAttribute(Type type, PermissionTarget target)
    {
        this.Type = type;
        this.Target = target;
    }
}
