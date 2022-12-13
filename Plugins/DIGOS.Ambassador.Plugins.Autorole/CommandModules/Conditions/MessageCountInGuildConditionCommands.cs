//
//  MessageCountInGuildConditionCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) Jarl Gullberg
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
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Model.Conditions;
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules;

public partial class AutoroleCommands
{
    public partial class AutoroleConditionCommands
    {
        /// <summary>
        /// Adds an instance of the condition to the role.
        /// </summary>
        /// <param name="autorole">The autorole configuration.</param>
        /// <param name="count">The message count.</param>
        [UsedImplicitly]
        [Command("add-total-messages")]
        [Description("Adds an instance of the condition to the role.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> AddConditionAsync
        (
            [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole,
            long count
        )
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var condition = _autoroles.CreateConditionProxy<MessageCountInGuildCondition>
            (
                guildID.Value,
                count
            )
            ?? throw new InvalidOperationException();

            var addCondition = await _autoroles.AddConditionAsync(autorole, condition);

            return !addCondition.IsSuccess
                ? Result<FeedbackMessage>.FromError(addCondition)
                : new FeedbackMessage("Condition added.", _feedback.Theme.Secondary);
        }

        /// <summary>
        /// Modifies an instance of the condition on the role.
        /// </summary>
        /// <param name="autorole">The autorole configuration.</param>
        /// <param name="conditionID">The ID of the condition.</param>
        /// <param name="count">The message count.</param>
        [UsedImplicitly]
        [Command("set-total-messages")]
        [Description("Modifies an instance of the condition on the role.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> ModifyConditionAsync
        (
            [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole,
            long conditionID,
            long count
        )
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var getCondition = Services.AutoroleService.GetCondition<MessageCountInGuildCondition>
            (
                autorole,
                conditionID
            );

            if (!getCondition.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getCondition);
            }

            var condition = getCondition.Entity;
            var modifyResult = await _autoroles.ModifyConditionAsync
            (
                condition,
                c =>
                {
                    c.RequiredCount = count;
                    c.SourceID = guildID.Value;
                }
            );

            return !modifyResult.IsSuccess
                ? Result<FeedbackMessage>.FromError(modifyResult)
                : new FeedbackMessage("Condition updated.", _feedback.Theme.Secondary);
        }
    }
}
