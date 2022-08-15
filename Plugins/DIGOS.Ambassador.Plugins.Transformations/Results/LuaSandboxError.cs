//
//  LuaSandboxError.cs
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

using JetBrains.Annotations;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Transformations.Results;

/// <summary>
/// Represents a lua error, where a function not on the sandbox's whitelist was called.
/// </summary>
/// <param name="ForbiddenFunction">The name of the forbidden function.</param>
[PublicAPI]
public record LuaSandboxError(string ForbiddenFunction) : ResultError($"Usage of {ForbiddenFunction} is prohibited.");
