//
//  AutoroleCondition.cs
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Database.Entities;
using JetBrains.Annotations;
using Remora.Discord.Core;
using Remora.Results;

namespace DIGOS.Ambassador.Plugins.Autorole.Model.Conditions.Bases
{
    /// <summary>
    /// Represents the base class for autorole conditions.
    /// </summary>
    [PublicAPI]
    [Table("AutoroleConditions", Schema = "AutoroleModule")]
    public abstract class AutoroleCondition : EFEntity, IAutoroleCondition
    {
        /// <inheritdoc />
        public abstract string GetDescriptiveUIText();

        /// <inheritdoc />
        public abstract bool HasSameConditionsAs(IAutoroleCondition autoroleCondition);

        /// <inheritdoc/>
        public abstract Task<Result<bool>> IsConditionFulfilledForUserAsync
        (
            IServiceProvider services,
            Snowflake guildID,
            Snowflake userID,
            CancellationToken ct = default
        );
    }
}
