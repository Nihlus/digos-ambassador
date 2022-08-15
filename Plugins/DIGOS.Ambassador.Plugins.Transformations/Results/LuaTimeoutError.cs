//
//  LuaTimeoutError.cs
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
/// Represents a fault in lua scripting, wherein the script took too long to complete.
/// </summary>
/// <remarks>
/// The timeout is not measured in wall clock time; rather, it is a limit on how many VM instructions are allowed to
/// run.
/// </remarks>
[PublicAPI]
public record LuaTimeoutError() : ResultError("Timed out while waiting for the script to complete.");
