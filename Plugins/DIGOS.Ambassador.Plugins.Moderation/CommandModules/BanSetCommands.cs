//
//  BanSetCommands.cs
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
using DIGOS.Ambassador.Plugins.Moderation.Permissions;
using DIGOS.Ambassador.Plugins.Moderation.Services;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using DIGOS.Ambassador.Plugins.Permissions.Model;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Extensions;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Moderation.CommandModules;

public partial class BanCommands
{
    /// <summary>
    /// Ban setter commands.
    /// </summary>
    [Group("set")]
    public class BanSetCommands : CommandGroup
    {
        private readonly BanService _bans;
        private readonly ICommandContext _context;
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="BanSetCommands"/> class.
        /// </summary>
        /// <param name="bans">The moderation service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="feedback">The feedback service.</param>
        public BanSetCommands
        (
            BanService bans,
            ICommandContext context,
            FeedbackService feedback
        )
        {
            _bans = bans;
            _context = context;
            _feedback = feedback;
        }

        /// <summary>
        /// Sets the reason for the ban.
        /// </summary>
        /// <param name="banID">The ID of the ban to edit.</param>
        /// <param name="newReason">The new reason for the ban.</param>
        [Command("reason")]
        [Description("Sets the reason for the ban.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<FeedbackMessage>> SetBanReasonAsync(long banID, string newReason)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var getBan = await _bans.GetBanAsync(guildID.Value, banID);
            if (!getBan.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getBan);
            }

            var ban = getBan.Entity;

            var setContents = await _bans.SetBanReasonAsync(ban, newReason);
            return setContents.IsSuccess
                ? new FeedbackMessage("Ban reason updated.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(setContents);
        }

        /// <summary>
        /// Sets the contextually relevant message for the ban.
        /// </summary>
        /// <param name="banID">The ID of the ban to edit.</param>
        /// <param name="newMessage">The new reason for the ban.</param>
        [Command("context-message")]
        [Description("Sets the contextually relevant message for the ban.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<FeedbackMessage>> SetBanContextMessageAsync(long banID, IMessage newMessage)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var getBan = await _bans.GetBanAsync(guildID.Value, banID);
            if (!getBan.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getBan);
            }

            var ban = getBan.Entity;

            var setMessage = await _bans.SetBanContextMessageAsync(ban, newMessage.ID);
            return setMessage.IsSuccess
                ? new FeedbackMessage("Ban context message updated.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(setMessage);
        }

        /// <summary>
        /// Sets the duration of the ban.
        /// </summary>
        /// <param name="banID">The ID of the ban to edit.</param>
        /// <param name="newDuration">The new duration of the ban.</param>
        [Command("duration")]
        [Description("Sets the duration of the ban.")]
        [RequirePermission(typeof(ManageBans), PermissionTarget.All)]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<FeedbackMessage>> SetBanDurationAsync(long banID, TimeSpan newDuration)
        {
            if (!_context.TryGetGuildID(out var guildID))
            {
                throw new InvalidOperationException();
            }

            var getBan = await _bans.GetBanAsync(guildID.Value, banID);
            if (!getBan.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getBan);
            }

            var ban = getBan.Entity;

            var newExpiration = ban.CreatedAt.Add(newDuration);

            var setExpiration = await _bans.SetBanExpiryDateAsync(ban, newExpiration);
            return setExpiration.IsSuccess
                ? new FeedbackMessage("Ban duration updated.", _feedback.Theme.Secondary)
                : Result<FeedbackMessage>.FromError(setExpiration);
        }
    }
}
