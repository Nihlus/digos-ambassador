﻿//
//  DescriptionPriorityAttribute.cs
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

namespace DIGOS.Ambassador.Plugins.Transformations.Attributes;

/// <summary>
/// An attribute which marks a bodypart as chiral.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class DescriptionPriorityAttribute : Attribute
{
    /// <summary>
    /// Gets the priority of the description.
    /// </summary>
    public uint Priority { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="DescriptionPriorityAttribute"/> class.
    /// </summary>
    /// <param name="priority">The priority of the description. The higher the value, the higher the priority.</param>
    public DescriptionPriorityAttribute(uint priority)
    {
        this.Priority = priority;
    }
}
