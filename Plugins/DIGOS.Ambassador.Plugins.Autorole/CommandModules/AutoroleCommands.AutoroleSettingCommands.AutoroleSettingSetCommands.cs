//
//  AutoroleCommands.AutoroleSettingCommands.AutoroleSettingSetCommands.cs
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

using System.ComponentModel;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Autorole.Permissions;
using DIGOS.Ambassador.Plugins.Autorole.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Autorole.CommandModules
{
    public partial class AutoroleCommands
    {
        public partial class AutoroleSettingCommands
        {
            /// <summary>
            /// Contains commands for editing server-wide autorole settings.
            /// </summary>
            [UsedImplicitly]
            [Group("set")]
            [Description("Commands for editing server-wide autorole settings.")]
            public class AutoroleSettingSetCommands : CommandGroup
            {
                private readonly AutoroleService _autoroles;
                private readonly ICommandContext _context;

                /// <summary>
                /// Initializes a new instance of the <see cref="AutoroleSettingSetCommands"/> class.
                /// </summary>
                /// <param name="autoroles">The autorole service.</param>
                /// <param name="feedback">The feedback service.</param>
                /// <param name="context">The command context.</param>
                public AutoroleSettingSetCommands
                (
                    AutoroleService autoroles,
                    UserFeedbackService feedback,
                    ICommandContext context
                )
                {
                    _autoroles = autoroles;
                    _context = context;
                }

                /// <summary>
                /// Sets the confirmation notification channel.
                /// </summary>
                /// <param name="channel">The channel.</param>
                [UsedImplicitly]
                [Command("confirmation-notification-channel")]
                [Description("Sets the confirmation notification channel.")]
                [RequireContext(ChannelContext.Guild)]
                [RequirePermission(typeof(EditAutoroleServerSettings), PermissionTarget.Self)]
                public async Task<Result<UserMessage>> SetAffirmationNotificationChannel(IChannel channel)
                {
                    if (channel.Type is not ChannelType.GuildText)
                    {
                        return new UserError("That's not a text channel.");
                    }

                    var setResult = await _autoroles.SetAffirmationNotificationChannelAsync
                    (
                        _context.GuildID.Value,
                        channel.ID
                    );

                    if (!setResult.IsSuccess)
                    {
                        return Result<UserMessage>.FromError(setResult);
                    }

                    return new ConfirmationMessage("Channel set.");
                }
            }
        }
    }
}
