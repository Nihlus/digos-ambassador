//
//  UserWarning.cs
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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using DIGOS.Ambassador.Plugins.Core.Model.Users;
using DIGOS.Ambassador.Plugins.Moderation.Model.Bases;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Moderation.Model
{
    /// <summary>
    /// Represents a warning attached to a user.
    /// </summary>
    [PublicAPI]
    [Table("UserWarnings", Schema = "ModerationModule")]
    public class UserWarning : AuthoredUserEntity
    {
        /// <summary>
        /// Gets the reason for the warning.
        /// </summary>
        [NotNull, Required]
        public string Reason { get; internal set; }

        /// <summary>
        /// Gets the time at which the note was last updated.
        /// </summary>
        [Required]
        public DateTime UpdatedAt { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserWarning"/> class.
        /// </summary>
        /// <remarks>
        /// Required by EF Core.
        /// </remarks>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
        protected UserWarning()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserWarning"/> class.
        /// </summary>
        /// <param name="user">The user that the note is attached to.</param>
        /// <param name="author">The user that created the note.</param>
        /// <param name="reason">The content of the note.</param>
        [SuppressMessage("ReSharper", "VirtualMemberCallInConstructor", Justification = "Required by EF Core.")]
        public UserWarning([NotNull] User user, [NotNull] User author, [NotNull] string reason)
            : base(user, author)
        {
            this.Reason = reason;

            this.UpdatedAt = DateTime.UtcNow;
        }
    }
}
