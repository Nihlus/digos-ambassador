//
//  TimeSinceJoinConditionCommands.cs
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
using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules
{
    public partial class AutoroleCommands
    {
        public partial class AutoroleConditionCommands
        {
            /// <summary>
            /// Adds an instance of the condition to the role.
            /// </summary>
            /// <param name="autorole">The autorole configuration.</param>
            /// <param name="time">The required time.</param>
            [UsedImplicitly]
            [Command("add-time-since-join")]
            [Description("Adds an instance of the condition to the role.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> AddSinceJoinConditionAsync
            (
                AutoroleConfiguration autorole,
                TimeSpan time
            )
            {
                var condition = _autoroles.CreateConditionProxy<TimeSinceJoinCondition>(time)
                                ?? throw new InvalidOperationException();

                var addCondition = await _autoroles.AddConditionAsync(autorole, condition);

                return !addCondition.IsSuccess
                    ? Result<UserMessage>.FromError(addCondition)
                    : new ConfirmationMessage("Condition added.");
            }

            /// <summary>
            /// Modifies an instance of the condition on the role.
            /// </summary>
            /// <param name="autorole">The autorole configuration.</param>
            /// <param name="conditionID">The ID of the condition.</param>
            /// <param name="time">The required time.</param>
            [UsedImplicitly]
            [Command("set-time-since-join")]
            [Description("Modifies an instance of the condition on the role.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> ModifySinceJoinConditionAsync
            (
                AutoroleConfiguration autorole,
                long conditionID,
                TimeSpan time
            )
            {
                var getCondition = _autoroles.GetCondition<TimeSinceJoinCondition>
                (
                    autorole,
                    conditionID
                );

                if (!getCondition.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getCondition);
                }

                var condition = getCondition.Entity;
                var modifyResult = await _autoroles.ModifyConditionAsync
                (
                    condition,
                    c => { c.RequiredTime = time; }
                );

                return !modifyResult.IsSuccess
                    ? Result<UserMessage>.FromError(modifyResult)
                    : new ConfirmationMessage("Condition updated.");
            }
        }
    }
}
