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

using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Permissions;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
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
        public class RoleCommands : ModuleBase
        {
            private readonly UserFeedbackService _feedback;
            private readonly CharacterRoleService _characterRoles;

            /// <summary>
            /// Initializes a new instance of the <see cref="RoleCommands"/> class.
            /// </summary>
            /// <param name="feedbackService">The feedback service.</param>
            /// <param name="characterRoles">The character service.</param>
            public RoleCommands
            (
                UserFeedbackService feedbackService,
                CharacterRoleService characterRoles
            )
            {
                _feedback = feedbackService;
                _characterRoles = characterRoles;
            }

            /// <summary>
            /// Lists the available character roles.
            /// </summary>
            [UsedImplicitly]
            [Command("list")]
            [RequireContext(ContextType.Guild)]
            public async Task ListAvailableRolesAsync()
            {
                var getServerRolesResult = await _characterRoles.GetCharacterRolesAsync(this.Context.Guild);
                if (!getServerRolesResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getServerRolesResult.ErrorReason);
                    return;
                }

                var serverRoles = getServerRolesResult.Entity.ToList();

                var eb = _feedback.CreateEmbedBase();

                eb.WithTitle("Available character roles");
                eb.WithDescription
                (
                    "These are the roles you can apply to your characters to automatically switch you to that role " +
                    "when you assume the character.\n" +
                    "\n" +
                    "In order to avoid mentioning everyone that has the role, use the numerical ID or role name" +
                    " instead of the actual mention. The ID is listed below along with the role name."
                );

                if (!serverRoles.Any())
                {
                    eb.WithFooter("There aren't any character roles available in this server.");
                }
                else
                {
                    foreach (var characterRole in serverRoles)
                    {
                        var discordRole = this.Context.Guild.GetRole((ulong)characterRole.DiscordID);
                        if (discordRole is null)
                        {
                            continue;
                        }

                        var ef = new EmbedFieldBuilder();
                        ef.WithName($"{discordRole.Name} ({discordRole.Id})");

                        var roleStatus = characterRole.Access == RoleAccess.Open
                            ? "open to everyone"
                            : "restricted";

                        ef.WithValue($"*This role is {roleStatus}.*");

                        eb.AddField(ef);
                    }
                }

                await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
            }

            /// <summary>
            /// Creates a new character role linked to a Discord role.
            /// </summary>
            /// <param name="discordRole">The discord role.</param>
            /// <param name="access">The access for the role.</param>
            [UsedImplicitly]
            [Command("create")]
            [Summary("Creates a new character role linked to a Discord role.")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "You must be allowed to manage roles.")]
            public async Task CreateCharacterRoleAsync
            (
                IRole discordRole,
                RoleAccess access = RoleAccess.Open
            )
            {
                var createRoleResult = await _characterRoles.CreateCharacterRoleAsync
                (
                    discordRole,
                    access
                );

                if (!createRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, createRoleResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character role created.");
            }

            /// <summary>
            /// Deletes the character role for a given discord role.
            /// </summary>
            /// <param name="discordRole">The discord role.</param>
            [UsedImplicitly]
            [Alias("delete", "remove", "erase")]
            [Command("delete")]
            [Summary("Deletes the character role for a given discord role.")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "You must be allowed to manage roles.")]
            public async Task DeleteCharacterRoleAsync
            (
                IRole discordRole
            )
            {
                var getExistingRoleResult = await _characterRoles.GetCharacterRoleAsync(discordRole);
                if (!getExistingRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getExistingRoleResult.ErrorReason);
                    return;
                }

                var deleteRoleResult = await _characterRoles.DeleteCharacterRoleAsync(getExistingRoleResult.Entity);
                if (!deleteRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, deleteRoleResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character role deleted.");
            }

            /// <summary>
            /// Sets the access conditions for the given role.
            /// </summary>
            /// <param name="discordRole">The discord role.</param>
            /// <param name="access">The new access conditions.</param>
            [UsedImplicitly]
            [Command("access")]
            [Summary("Sets the access conditions for the given role.")]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageRoles, ErrorMessage = "You must be allowed to manage roles.")]
            public async Task SetCharacterRoleAccessAsync
            (
                IRole discordRole,
                RoleAccess access
            )
            {
                var getExistingRoleResult = await _characterRoles.GetCharacterRoleAsync(discordRole);
                if (!getExistingRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getExistingRoleResult.ErrorReason);
                    return;
                }

                var setRoleAccessResult = await _characterRoles.SetCharacterRoleAccessAsync
                (
                    getExistingRoleResult.Entity,
                    access
                );

                if (!setRoleAccessResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setRoleAccessResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync
                (
                    this.Context, "Character role access conditions set."
                );
            }

            /// <summary>
            /// Clears the role from a character.
            /// </summary>
            /// <param name="character">The character.</param>
            [UsedImplicitly]
            [Command("clear")]
            [Summary("Clears the role from a character.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task ClearCharacterRoleAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character
            )
            {
                var result = await _characterRoles.ClearCharacterRoleAsync((IGuildUser)this.Context.User, character);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character role cleared.");
            }
        }
    }
}
