﻿//
//  KinkPreference.cs
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

using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Kinks.Model;

/// <summary>
/// Represents a user's preference for a certain sexual kink or fetish.
/// </summary>
[PublicAPI]
public enum KinkPreference
{
    /// <summary>
    /// The user has no preference, either for or against this kink.
    /// </summary>
    NoPreference,

    /// <summary>
    /// The user will not participate in or reciprocate this kink.
    /// </summary>
    No,

    /// <summary>
    /// The user may be open to participating in or reciprocating this kink, depending on other factors.
    /// </summary>
    Maybe,

    /// <summary>
    /// The user would participate in or reciprocate this kink.
    /// </summary>
    Like,

    /// <summary>
    /// The user is very interested in this kink.
    /// </summary>
    Favourite
}
