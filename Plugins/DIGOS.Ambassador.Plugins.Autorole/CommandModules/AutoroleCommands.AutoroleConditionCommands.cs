//
//  AutoroleCommands.AutoroleConditionCommands.cs
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

using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Plugins.Autorole.Model;
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules;

public partial class AutoroleCommands
{
    /// <summary>
    /// Grouping module for condition-specific commands.
    /// </summary>
    [Group("condition")]
    public partial class AutoroleConditionCommands : CommandGroup
    {
        private readonly AutoroleService _autoroles;
        private readonly ICommandContext _context;
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoroleConditionCommands"/> class.
        /// </summary>
        /// <param name="autoroles">The autorole service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="feedback">The feedback service.</param>
        public AutoroleConditionCommands
        (
            AutoroleService autoroles,
            ICommandContext context,
            FeedbackService feedback
        )
        {
            _autoroles = autoroles;
            _context = context;
            _feedback = feedback;
        }

        /// <summary>
        /// Removes a condition from a role.
        /// </summary>
        /// <param name="autorole">The autorole.</param>
        /// <param name="conditionID">The ID of the condition to remove.</param>
        [UsedImplicitly]
        [Command("remove")]
        [Description("Removes the condition from the role.")]
        [RequirePermission(typeof(EditAutorole), PermissionTarget.Self)]
        public async Task<Result<FeedbackMessage>> RemoveConditionAsync
        (
            [DiscordTypeHint(TypeHint.Role)] AutoroleConfiguration autorole,
            long conditionID
        )
        {
            var removeCondition = await _autoroles.RemoveConditionAsync(autorole, conditionID);

            return !removeCondition.IsSuccess
                ? Result<FeedbackMessage>.FromError(removeCondition)
                : new FeedbackMessage("Condition removed.", _feedback.Theme.Secondary);
        }
    }
}
