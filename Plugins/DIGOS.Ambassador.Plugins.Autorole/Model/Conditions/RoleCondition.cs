//
//  RoleCondition.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases;
using Discord;
using JetBrains.Annotations;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions
{
    /// <summary>
    /// Represents a required role.
    /// </summary>
    [PublicAPI]
    public class RoleCondition : AutoroleCondition
    {
        /// <summary>
        /// Gets the ID of the Discord role.
        /// </summary>
        public long RoleID { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleCondition"/> class.
        /// </summary>
        /// <param name="roleID">The ID of the Discord role.</param>
        [UsedImplicitly]
        protected RoleCondition(long roleID)
        {
            this.RoleID = roleID;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RoleCondition"/> class.
        /// </summary>
        /// <param name="role">The role.</param>
        public RoleCondition(IRole role)
            : this((long)role.Id)
        {
        }

        /// <inheritdoc />
        public override string GetDescriptiveUIText()
        {
            return $"Has the {MentionUtils.MentionRole((ulong)this.RoleID)} role";
        }

        /// <inheritdoc />
        public override bool HasSameConditionsAs(IAutoroleCondition autoroleCondition)
        {
            if (!(autoroleCondition is RoleCondition roleCondition))
            {
                return false;
            }

            return this.RoleID == roleCondition.RoleID;
        }

        /// <inheritdoc/>
        public override Task<RetrieveEntityResult<bool>> IsConditionFulfilledForUser(IServiceProvider services, IGuildUser discordUser)
        {
            return Task.FromResult
            (
                RetrieveEntityResult<bool>.FromSuccess(discordUser.RoleIds.Contains((ulong)this.RoleID))
            );
        }
    }
}
