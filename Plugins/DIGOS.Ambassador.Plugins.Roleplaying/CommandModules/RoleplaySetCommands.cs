//
//  RoleplaySetCommands.cs
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
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Roleplaying.Model;
using DIGOS.Ambassador.Plugins.Roleplaying.Services;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;

using FeedbackMessage = Remora.Discord.Commands.Feedback.Messages.FeedbackMessage;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Roleplaying.CommandModules
{
    public partial class RoleplayCommands
    {
        /// <summary>
        /// Setter commands for roleplay properties.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : CommandGroup
        {
            private readonly RoleplayDiscordService _discordRoleplays;
            private readonly FeedbackService _feedback;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="discordRoleplays">The roleplay service.</param>
            /// <param name="feedback">The feedback service.</param>
            public SetCommands
            (
                RoleplayDiscordService discordRoleplays, FeedbackService feedback)
            {
                _discordRoleplays = discordRoleplays;
                _feedback = feedback;
            }

            /// <summary>
            /// Sets the name of the named roleplay.
            /// </summary>
            /// <param name="newRoleplayName">The roleplay's new name.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("name")]
            [Description("Sets the new name of the named roleplay.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<FeedbackMessage>> SetRoleplayNameAsync
            (
                string newRoleplayName,
                [RequireEntityOwner]
                [AutocompleteProvider("roleplay::owned")]
                Roleplay roleplay
            )
            {
                var result = await _discordRoleplays.SetRoleplayNameAsync
                (
                    roleplay,
                    newRoleplayName
                );

                return !result.IsSuccess
                    ? Result<FeedbackMessage>.FromError(result)
                    : new FeedbackMessage("Roleplay name set.", _feedback.Theme.Secondary);
            }

            /// <summary>
            /// Sets the summary of the named roleplay.
            /// </summary>
            /// <param name="newRoleplaySummary">The roleplay's new summary.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("summary")]
            [Description("Sets the summary of the named roleplay.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<FeedbackMessage>> SetRoleplaySummaryAsync
            (
                string newRoleplaySummary,
                [RequireEntityOwner]
                [AutocompleteProvider("roleplay::owned")]
                Roleplay roleplay
            )
            {
                var result = await _discordRoleplays.SetRoleplaySummaryAsync(roleplay, newRoleplaySummary);

                return !result.IsSuccess
                    ? Result<FeedbackMessage>.FromError(result)
                    : new FeedbackMessage("Roleplay summary set.", _feedback.Theme.Secondary);
            }

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is NSFW. This restricts which channels it
            /// can be made active in.
            /// </summary>
            /// <param name="isNSFW">true if the roleplay is NSFW; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("nsfw")]
            [Description("Sets a value indicating whether or not the named roleplay is NSFW.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<FeedbackMessage>> SetRoleplayIsNSFW
            (
                bool isNSFW,
                [RequireEntityOwner]
                [AutocompleteProvider("roleplay::owned")]
                Roleplay roleplay
            )
            {
                var result = await _discordRoleplays.SetRoleplayIsNSFWAsync(roleplay, isNSFW);

                return !result.IsSuccess
                    ? Result<FeedbackMessage>.FromError(result)
                    : new FeedbackMessage($"Roleplay set to {(isNSFW ? "NSFW" : "SFW")}", _feedback.Theme.Secondary);
            }

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is private. This restricts replays to participants.
            /// </summary>
            /// <param name="isPrivate">true if the roleplay is private; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("private")]
            [Description("Sets a value indicating whether or not the named roleplay is private.")]
            [RequireContext(ChannelContext.Guild)]
            public Task<Result<FeedbackMessage>> SetRoleplayIsPrivate
            (
                bool isPrivate,
                [RequireEntityOwner]
                [AutocompleteProvider("roleplay::owned")]
                Roleplay roleplay
            )
                => SetRoleplayIsPublic(!isPrivate, roleplay);

            /// <summary>
            /// Sets a value indicating whether or not the named roleplay is public. This restricts replays to
            /// participants.
            /// </summary>
            /// <param name="isPublic">true if the roleplay is public; otherwise, false.</param>
            /// <param name="roleplay">The roleplay.</param>
            [UsedImplicitly]
            [Command("public")]
            [Description("Sets a value indicating whether or not the named roleplay is public.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<FeedbackMessage>> SetRoleplayIsPublic
            (
                bool isPublic,
                [RequireEntityOwner]
                [AutocompleteProvider("roleplay::owned")]
                Roleplay roleplay
            )
            {
                var result = await _discordRoleplays.SetRoleplayIsPublicAsync(roleplay, isPublic);
                if (!result.IsSuccess)
                {
                    return Result<FeedbackMessage>.FromError(result);
                }

                return new FeedbackMessage
                (
                    $"Roleplay set to {(isPublic ? "public" : "private")}",
                    _feedback.Theme.Secondary
                );
            }
        }
    }
}
