//
//  AutoroleConfirmation.cs
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

namespace DIGOS.Ambassador.Plugins.Autorole.Model
{
    /// <summary>
    /// Represents an externally initiated confirmation of a user's qualification for an autorole.
    /// </summary>
    [PublicAPI]
    [Table("AutoroleConfirmations", Schema = "AutoroleModule")]
    public class AutoroleConfirmation : EFEntity
    {
        /// <summary>
        /// Gets the autorole that the confirmation is for.
        /// </summary>
        public virtual AutoroleConfiguration Autorole { get; private set; } = null!;

        /// <summary>
        /// Gets the user that the confirmation is for.
        /// </summary>
        public virtual User User { get; private set; } = null!;

        /// <summary>
        /// Gets a value indicating whether the user's qualification has been confirmed.
        /// </summary>
        public bool IsConfirmed { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleConfirmation"/> class.
        /// </summary>
        [SuppressMessage("ReSharper", "NotNullMemberIsNotInitialized", Justification = "Initialized by EF Core.")]
        protected AutoroleConfirmation()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleConfirmation"/> class.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="user">The user.</param>
        /// <param name="isConfirmed">Whether the user is conformed or not.</param>
        public AutoroleConfirmation(AutoroleConfiguration autorole, User user, bool isConfirmed = false)
        {
            this.Autorole = autorole;
            this.User = user;
            this.IsConfirmed = isConfirmed;
        }
    }
}
