//
//  CharacterCommands.RoleCommands.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.Pagination.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Permissions;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Abstractions.Rest;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Characters.CommandModules
{
    public partial class CharacterCommands
    {
        /// <summary>
        /// Role-related commands.
        /// </summary>
        [UsedImplicitly]
        [Group("role")]
        public class RoleCommands : CommandGroup
        {
            private readonly FeedbackService _feedback;
            private readonly CharacterRoleService _characterRoles;
            private readonly InteractivityService _interactivity;
            private readonly IDiscordRestGuildAPI _guildAPI;
            private readonly ICommandContext _context;

            /// <summary>
            /// Initializes a new instance of the <see cref="RoleCommands"/> class.
            /// </summary>
            /// <param name="feedbackService">The feedback service.</param>
            /// <param name="characterRoles">The character service.</param>
            /// <param name="context">The command context.</param>
            /// <param name="interactivity">The interactivity service.</param>
            /// <param name="guildAPI">The guild API.</param>
            public RoleCommands
            (
                FeedbackService feedbackService,
                CharacterRoleService characterRoles,
                ICommandContext context,
                InteractivityService interactivity,
                IDiscordRestGuildAPI guildAPI
            )
            {
                _feedback = feedbackService;
                _characterRoles = characterRoles;
                _context = context;
                _interactivity = interactivity;
                _guildAPI = guildAPI;
            }

            /// <summary>
            /// Lists the available character roles.
            /// </summary>
            [UsedImplicitly]
            [Command("list-available")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result> ListAvailableRolesAsync()
            {
                var baseEmbed = new Embed
                {
                    Colour = _feedback.Theme.Secondary,
                    Title = "Available character roles",
                    Description = "These are the roles you can apply to your characters to automatically switch you " +
                                  "to that role when you assume the character.\n" +
                                  "\n" +
                                  "In order to avoid mentioning everyone that has the role, use the numerical ID or " +
                                  "role name instead of the actual mention. The ID is listed below along with the " +
                                  "role name."
                };

                var getCharacterRoles = await _characterRoles.GetCharacterRolesAsync(_context.GuildID.Value);
                if (!getCharacterRoles.IsSuccess)
                {
                    return Result.FromError(getCharacterRoles);
                }

                var characterRoles = getCharacterRoles.Entity;

                if (!characterRoles.Any())
                {
                    baseEmbed = baseEmbed with
                    {
                        Footer = new EmbedFooter("There aren't any character roles available in this server.")
                    };

                    return await _interactivity.SendContextualInteractiveMessageAsync
                    (
                        _context.User.ID,
                        new[] { baseEmbed },
                        ct: this.CancellationToken
                    );
                }

                var getGuildRoles = await _guildAPI.GetGuildRolesAsync(_context.GuildID.Value, this.CancellationToken);
                if (!getGuildRoles.IsSuccess)
                {
                    return Result.FromError(getGuildRoles);
                }

                var guildRoles = getGuildRoles.Entity;

                var fields = characterRoles.Select
                (
                    r =>
                    {
                        var guildRole = guildRoles.FirstOrDefault(gr => gr.ID == r.DiscordID);

                        var roleStatus = r.Access == RoleAccess.Open
                            ? "open to everyone"
                            : "restricted";

                        string name = guildRole is null
                            ? $"??? ({r.DiscordID} - this role appears to be deleted.)"
                            : $"{guildRole.Name} ({r.DiscordID})";

                        var value = $"*This role is {roleStatus}.*";

                        return new EmbedField(name, value);
                    }
                );

                var pages = PageFactory.FromFields(fields, pageBase: baseEmbed);

                return await _interactivity.SendContextualInteractiveMessageAsync
                (
                    _context.User.ID,
                    pages,
                    ct: this.CancellationToken
                );
            }

            /// <summary>
            /// Creates a new character role linked to a Discord role.
            /// </summary>
            /// <param name="discordRole">The discord role.</param>
            /// <param name="access">The access for the role.</param>
            [UsedImplicitly]
            [Command("create")]
            [Description("Creates a new character role linked to a Discord role.")]
            [RequireContext(ChannelContext.Guild)]
            [RequireUserGuildPermission(DiscordPermission.ManageRoles)]
            public async Task<Result<FeedbackMessage>> CreateCharacterRoleAsync
            (
                IRole discordRole,
                RoleAccess access = RoleAccess.Open
            )
            {
                var createRoleResult = await _characterRoles.CreateCharacterRoleAsync
                (
                    _context.GuildID.Value,
                    discordRole.ID,
                    access,
                    this.CancellationToken
                );

                return !createRoleResult.IsSuccess
                    ? Result<FeedbackMessage>.FromError(createRoleResult)
                    : new FeedbackMessage("Character role created.", _feedback.Theme.Secondary);
            }

            /// <summary>
            /// Deletes the character role for a given discord role.
            /// </summary>
            /// <param name="discordRole">The discord role.</param>
            [UsedImplicitly]
            [Command("delete")]
            [Description("Deletes the character role for a given discord role.")]
            [RequireContext(ChannelContext.Guild)]
            [RequireUserGuildPermission(DiscordPermission.ManageRoles)]
            public async Task<Result<FeedbackMessage>> DeleteCharacterRoleAsync(IRole discordRole)
            {
                var getExistingRoleResult = await _characterRoles.GetCharacterRoleAsync
                (
                    _context.GuildID.Value,
                    discordRole.ID,
                    this.CancellationToken
                );

                if (!getExistingRoleResult.IsSuccess)
                {
                    return Result<FeedbackMessage>.FromError(getExistingRoleResult);
                }

                var deleteRoleResult = await _characterRoles.DeleteCharacterRoleAsync
                (
                    getExistingRoleResult.Entity,
                    this.CancellationToken
                );

                return !deleteRoleResult.IsSuccess
                    ? Result<FeedbackMessage>.FromError(deleteRoleResult)
                    : new FeedbackMessage("Character role deleted.", _feedback.Theme.Secondary);
            }

            /// <summary>
            /// Sets the access conditions for the given role.
            /// </summary>
            /// <param name="discordRole">The discord role.</param>
            /// <param name="access">The new access conditions.</param>
            [UsedImplicitly]
            [Command("set-access")]
            [Description("Sets the access conditions for the given role.")]
            [RequireContext(ChannelContext.Guild)]
            [RequireUserGuildPermission(DiscordPermission.ManageRoles)]
            public async Task<Result<FeedbackMessage>> SetCharacterRoleAccessAsync(IRole discordRole, RoleAccess access)
            {
                var getExistingRoleResult = await _characterRoles.GetCharacterRoleAsync
                (
                    _context.GuildID.Value,
                    discordRole.ID,
                    this.CancellationToken
                );

                if (!getExistingRoleResult.IsSuccess)
                {
                    return Result<FeedbackMessage>.FromError(getExistingRoleResult);
                }

                var setRoleAccessResult = await _characterRoles.SetCharacterRoleAccessAsync
                (
                    getExistingRoleResult.Entity,
                    access,
                    this.CancellationToken
                );

                return !setRoleAccessResult.IsSuccess
                    ? Result<FeedbackMessage>.FromError(setRoleAccessResult)
                    : new FeedbackMessage("Character role access conditions set.", _feedback.Theme.Secondary);
            }

            /// <summary>
            /// Clears the role from a character.
            /// </summary>
            /// <param name="character">The character.</param>
            [UsedImplicitly]
            [Command("clear")]
            [Description("Clears the role from a character.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<FeedbackMessage>> ClearCharacterRoleAsync
            (
                [RequireEntityOwner]
                Character character
            )
            {
                var result = await _characterRoles.ClearCharacterRoleAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    character,
                    this.CancellationToken
                );

                return !result.IsSuccess
                    ? Result<FeedbackMessage>.FromError(result)
                    : new FeedbackMessage("Character role cleared.", _feedback.Theme.Secondary);
            }
        }
    }
}
