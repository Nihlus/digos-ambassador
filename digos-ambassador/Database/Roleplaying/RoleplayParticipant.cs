//
//  RoleplayParticipant.cs
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

using System.ComponentModel.DataAnnotations;
using DIGOS.Ambassador.Database.Interfaces;
using DIGOS.Ambassador.Database.Users;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Database.Roleplaying
{
    /// <summary>
    /// Represents a join entry for a user that has participated in a roleplay in any way.
    /// </summary>
    public class RoleplayParticipant : IEFEntity
    {
        /// <inheritdoc />
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the roleplay that the user is a part of.
        /// </summary>
        [Required]
        public Roleplay Roleplay { get; set; }

        /// <summary>
        /// Gets or sets the user that is part of the roleplay.
        /// </summary>
        [Required]
        public User User { get; set; }

        /// <summary>
        /// Gets or sets the current status of the user in the roleplay.
        /// </summary>
        public ParticipantStatus Status { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayParticipant"/> class.
        /// </summary>
        public RoleplayParticipant()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayParticipant"/> class.
        /// </summary>
        /// <param name="roleplay">The roleplay that the user is participating in.</param>
        /// <param name="user">The user that is participating in the roleplay.</param>
        /// <param name="status">The status of the user.</param>
        public RoleplayParticipant([NotNull] Roleplay roleplay, [NotNull] User user, ParticipantStatus status)
        {
            this.Roleplay = roleplay;
            this.User = user;
            this.Status = status;
        }
    }
}
