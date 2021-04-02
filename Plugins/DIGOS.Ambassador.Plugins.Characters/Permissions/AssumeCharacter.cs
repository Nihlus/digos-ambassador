//
//  AssumeCharacter.cs
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

namespace DIGOS.Ambassador.Plugins.Characters.Permissions
{
    /// <summary>
    /// Represents a permission that allows a user to assume characters.
    /// </summary>
    public sealed class AssumeCharacter : Permission
    {
        /// <inheritdoc />
        public override Guid UniqueIdentifier { get; } = new Guid("01051A8C-A038-4F15-ABFB-368FC1564BDA");

        /// <inheritdoc />
        public override string FriendlyName => nameof(AssumeCharacter);

        /// <inheritdoc />
        public override string Description => "Allows you to assume characters.";

        /// <inheritdoc />
        public override bool IsGrantedByDefaultToSelf => true;
    }
}
