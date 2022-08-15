//
//  ParticipantStatus.cs
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

namespace DIGOS.Ambassador.Plugins.Roleplaying.Model;

/// <summary>
/// Represents the status of a participant in a roleplay.
/// </summary>
public enum ParticipantStatus
{
    /// <summary>
    /// The user isn't participating in the roleplay. They may have been a part of it previously, but isn't anymore.
    /// </summary>
    None,

    /// <summary>
    /// The user has been invited to the roleplay, but has not yet joined.
    /// </summary>
    Invited,

    /// <summary>
    /// The user has joined the roleplay.
    /// </summary>
    Joined,

    /// <summary>
    /// The user has been kicked from the roleplay, and cannot rejoin unless reinvited.
    /// </summary>
    Kicked
}
