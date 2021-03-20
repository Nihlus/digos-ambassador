//
//  CharacterCommands.SetCommands.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
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
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Characters.CommandModules
{
    public partial class CharacterCommands
    {
        /// <summary>
        /// Property setter commands for characters.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : CommandGroup
        {
            private readonly CharacterDiscordService _characters;
            private readonly CharacterRoleService _characterRoles;
            private readonly ICommandContext _context;
            private readonly IDiscordRestGuildAPI _guildAPI;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="characterService">The character service.</param>
            /// <param name="characterRoles">The character role service.</param>
            /// <param name="context">The command context.</param>
            /// <param name="guildAPI">The guild API.</param>
            public SetCommands
            (
                CharacterDiscordService characterService,
                CharacterRoleService characterRoles,
                ICommandContext context,
                IDiscordRestGuildAPI guildAPI
            )
            {
                _characters = characterService;
                _characterRoles = characterRoles;
                _context = context;
                _guildAPI = guildAPI;
            }

            /// <summary>
            /// Sets the name of a character.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterName">The new name of the character.</param>
            [UsedImplicitly]
            [Command("name")]
            [Description("Sets the name of a character.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterNameAsync
            (
                [RequireEntityOwner]
                Character character,
                string newCharacterName
            )
            {
                var setNameResult = await _characters.SetCharacterNameAsync(character, newCharacterName);
                return !setNameResult.IsSuccess
                    ? Result<UserMessage>.FromError(setNameResult)
                    : new ConfirmationMessage("Character name set.");
            }

            /// <summary>
            /// Sets the avatar of a character. You can attach an image instead of passing a url as a parameter.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterAvatarUrl">The url of the new avatar. Optional.</param>
            [UsedImplicitly]
            [Command("avatar")]
            [Description("Sets the avatar of a character. You can attach an image instead of passing a url as a parameter.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterAvatarAsync
            (
                [RequireEntityOwner]
                Character character,
                string? newCharacterAvatarUrl = null
            )
            {
                if (newCharacterAvatarUrl is null)
                {
                    if (_context is not MessageContext messageContext)
                    {
                        return new UserError("Images can't be added via slash commands. This is a discord limitation.");
                    }

                    var attachments = messageContext.Message.Attachments;
                    var hasAtLeastOneAttachment = attachments.HasValue && attachments.Value.Any();
                    if (!hasAtLeastOneAttachment)
                    {
                        return new UserError("You need to attach an image or provide a url.");
                    }

                    var newAvatar = attachments.Value!.First();
                    newCharacterAvatarUrl = newAvatar.Url;
                }

                var galleryImage = character.Images.FirstOrDefault
                (
                    i => i.Name.ToLower().Equals(newCharacterAvatarUrl.ToLower())
                );

                if (!(galleryImage is null))
                {
                    newCharacterAvatarUrl = galleryImage.Url;
                }

                var result = await _characters.SetCharacterAvatarAsync
                (
                    character,
                    newCharacterAvatarUrl ?? throw new ArgumentNullException(nameof(newCharacterAvatarUrl))
                );

                return !result.IsSuccess
                    ? Result<UserMessage>.FromError(result)
                    : new ConfirmationMessage("Character avatar set.");
            }

            /// <summary>
            /// Sets the nickname that the user should have when a character is active.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterNickname">The new nickname of the character. Max 32 characters.</param>
            [UsedImplicitly]
            [Command("nickname")]
            [Description("Sets the nickname that the user should have when the character is active.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterNicknameAsync
            (
                [RequireEntityOwner]
                Character character,
                string newCharacterNickname
            )
            {
                var setNickResult = await _characters.SetCharacterNicknameAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    character,
                    newCharacterNickname
                );

                return !setNickResult.IsSuccess
                    ? Result<UserMessage>.FromError(setNickResult)
                    : new ConfirmationMessage("Character nickname set.");
            }

            /// <summary>
            /// Sets the summary of a character.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterSummary">The new summary. Max 240 characters.</param>
            [UsedImplicitly]
            [Command("summary")]
            [Description("Sets the summary of a character.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterSummaryAsync
            (
                [RequireEntityOwner]
                Character character,
                string newCharacterSummary
            )
            {
                var setSummaryResult = await _characters.SetCharacterSummaryAsync(character, newCharacterSummary);

                return !setSummaryResult.IsSuccess
                    ? Result<UserMessage>.FromError(setSummaryResult)
                    : new ConfirmationMessage("Character summary set.");
            }

            /// <summary>
            /// Sets the description of a character.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterDescription">The new description of the character. Optional.</param>
            [UsedImplicitly]
            [Command("description")]
            [Description("Sets the description of a character.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterDescriptionAsync
            (
                [RequireEntityOwner]
                Character character,
                string newCharacterDescription
            )
            {
                var setDescriptionResult = await _characters.SetCharacterDescriptionAsync
                (
                    character,
                    newCharacterDescription
                );

                return !setDescriptionResult.IsSuccess
                    ? Result<UserMessage>.FromError(setDescriptionResult)
                    : new ConfirmationMessage("Character description set.");
            }

            /// <summary>
            /// Sets whether or not a character is NSFW.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="isNSFW">Whether or not the character is NSFW.</param>
            [UsedImplicitly]
            [Command("is-nsfw")]
            [Description("Sets whether or not a character is NSFW.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterIsNSFWAsync
            (
                [RequireEntityOwner]
                Character character,
                bool isNSFW
            )
            {
                var setNSFW = await _characters.SetCharacterIsNSFWAsync(character, isNSFW);

                return !setNSFW.IsSuccess
                    ? Result<UserMessage>.FromError(setNSFW)
                    : new ConfirmationMessage($"Character set to {(isNSFW ? "NSFW" : "SFW")}.");
            }

            /// <summary>
            /// Sets the preferred pronoun for a character.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="pronounFamily">The pronoun family.</param>
            [UsedImplicitly]
            [Command("pronouns")]
            [Description("Sets the preferred pronoun of a character.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterPronounAsync
            (
                [RequireEntityOwner]
                Character character,
                string pronounFamily
            )
            {
                var result = await _characters.SetCharacterPronounsAsync(character, pronounFamily);

                return !result.IsSuccess
                    ? Result<UserMessage>.FromError(result)
                    : new ConfirmationMessage("Preferred pronoun set.");
            }

            /// <summary>
            /// Sets the given character's display role.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="discordRole">The role.</param>
            [UsedImplicitly]
            [Command("role")]
            [Description("Sets the given character's display role.")]
            [RequireContext(ChannelContext.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task<Result<UserMessage>> SetCharacterRoleAsync
            (
                [RequireEntityOwner]
                Character character,
                IRole discordRole
            )
            {
                var getRoleResult = await _characterRoles.GetCharacterRoleAsync
                (
                    _context.GuildID.Value,
                    discordRole.ID,
                    this.CancellationToken
                );

                if (!getRoleResult.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getRoleResult);
                }

                // Get a bunch of stuff for permission checking...
                var getMember = await _guildAPI.GetGuildMemberAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    this.CancellationToken
                );

                if (!getMember.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getMember);
                }

                var member = getMember.Entity;

                var getGuildRoles = await _guildAPI.GetGuildRolesAsync(_context.GuildID.Value, this.CancellationToken);
                if (!getGuildRoles.IsSuccess)
                {
                    return Result<UserMessage>.FromError(getGuildRoles);
                }

                var guildRoles = getGuildRoles.Entity;
                var everyoneRole = guildRoles.First(r => r.ID == _context.GuildID.Value);
                var memberRoles = guildRoles.Where(r => member.Roles.Contains(r.ID)).ToList();

                // We ignore channel overrides here; the user should have it on a guild level
                var computedPermissions = DiscordPermissionSet.ComputePermissions
                (
                    _context.User.ID,
                    everyoneRole,
                    memberRoles
                );

                var characterRole = getRoleResult.Entity;
                if (characterRole.Access == RoleAccess.Restricted)
                {
                    if (!computedPermissions.HasPermission(DiscordPermission.ManageRoles))
                    {
                        return new UserError
                        (
                            "That role is restricted, and you must be able to manage roles to use it."
                        );
                    }
                }

                var setRoleResult = await _characterRoles.SetCharacterRoleAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    character,
                    characterRole,
                    this.CancellationToken
                );

                return !setRoleResult.IsSuccess
                    ? Result<UserMessage>.FromError(setRoleResult)
                    : new ConfirmationMessage("Character role set.");
            }

            /// <summary>
            /// Sets your default form to your current character.
            /// </summary>
            [UsedImplicitly]
            [Command("default-to-current")]
            [Description("Sets your default form to your current character.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetDefaultCharacterAsync()
            {
                var result = await _characters.GetCurrentCharacterAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    this.CancellationToken
                );

                if (!result.IsSuccess)
                {
                    return Result<UserMessage>.FromError(result);
                }

                return await SetDefaultCharacterAsync(result.Entity);
            }

            /// <summary>
            /// Sets your default form to the given character.
            /// </summary>
            /// <param name="character">The character to set as the default character.</param>
            [UsedImplicitly]
            [Command("default")]
            [Description("Sets your default form to the given character.")]
            [RequireContext(ChannelContext.Guild)]
            public async Task<Result<UserMessage>> SetDefaultCharacterAsync
            (
                [RequireEntityOwner]
                Character character
            )
            {
                var result = await _characters.SetDefaultCharacterAsync
                (
                    _context.GuildID.Value,
                    _context.User.ID,
                    character,
                    this.CancellationToken
                );

                return !result.IsSuccess
                    ? Result<UserMessage>.FromError(result)
                    : new ConfirmationMessage("Default character set.");
            }
        }
    }
}
