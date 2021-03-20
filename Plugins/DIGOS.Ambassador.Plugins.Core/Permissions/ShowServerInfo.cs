//
// ShowServerInfo.cs
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

namespace DIGOS.Ambassador.Plugins.Core.Permissions
{
    /// <summary>
    /// Represents a permission that allows a user to view server information.
    /// </summary>
    public sealed class ShowServerInfo : Permission
    {
        /// <inheritdoc />
        public override Guid UniqueIdentifier { get; } = new Guid("9704A16F-D05E-48D6-8749-EB76E43DD7B6");

        /// <inheritdoc />
        public override string FriendlyName => nameof(ShowServerInfo);

        /// <inheritdoc />
        public override string Description => "Allows you to view server information.";

        /// <inheritdoc />
        public override bool IsGrantedByDefaultToSelf => true;

        /// <inheritdoc />
        public override bool IsGrantedByDefaultToOthers => true;
    }
}
