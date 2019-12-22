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

using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Core.Database.Entities;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using JetBrains.Annotations;

using NotNullAttribute = JetBrains.Annotations.NotNullAttribute;

namespace DIGOS.Ambassador.Plugins.Roleplaying.Model
{
    /// <summary>
    /// Represents a join entry for a user that has participated in a roleplay in any way.
    /// </summary>
    [PublicAPI]
    [Table("RoleplayParticipants", Schema = "RoleplayModule")]
    public class RoleplayParticipant : EFEntity
    {
        /// <summary>
        /// Gets the roleplay that the user is a part of.
        /// </summary>
        public virtual Roleplay Roleplay { get; private set; } = null!;

        /// <summary>
        /// Gets the user that is part of the roleplay.
        /// </summary>
        public virtual User User { get; private set; } = null!;

        /// <summary>
        /// Gets the current status of the user in the roleplay.
        /// </summary>
        public ParticipantStatus Status { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayParticipant"/> class.
        /// </summary>
        [UsedImplicitly]
        public RoleplayParticipant()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleplayParticipant"/> class.
        /// </summary>
        /// <param name="roleplay">The roleplay that the user is participating in.</param>
        /// <param name="user">The user that is participating in the roleplay.</param>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Used by EF Core proxies.")]
        public RoleplayParticipant
        (
            [NotNull] Roleplay roleplay,
            [NotNull] User user
        )
        {
            this.Roleplay = roleplay;
            this.User = user;

            this.Status = ParticipantStatus.None;
        }
    }
}
