//
// EditUserInfo.cs
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
using DIGOS.Ambassador.Plugins.Permissions;
using JetBrains.Annotations;

namespace DIGOS.Ambassador.Plugins.Core.Permissions
{
    /// <summary>
    /// Represents a permission that allows a user to edit user information.
    /// </summary>
    public sealed class EditUserInfo : Permission
    {
        /// <inheritdoc />
        public override Guid UniqueIdentifier { get; } = new Guid("CCDFAC99-4A25-4A37-A4E8-93C7F0C54323");

        /// <inheritdoc />
        public override string FriendlyName => nameof(EditUserInfo);

        /// <inheritdoc />
        public override string Description => "Allows you to edit user information.";

        /// <inheritdoc />
        public override bool IsGrantedByDefaultToSelf => true;
    }
}
