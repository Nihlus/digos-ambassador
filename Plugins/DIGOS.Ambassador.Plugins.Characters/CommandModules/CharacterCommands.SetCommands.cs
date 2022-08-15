//
//  CharacterCommands.SetCommands.cs
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Errors;
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
using Remora.Discord.Commands.Attributes;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Discord.Commands.Feedback.Messages;
using Remora.Discord.Commands.Feedback.Services;
using Remora.Results;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Characters.CommandModules;

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
        private readonly FeedbackService _feedback;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetCommands"/> class.
        /// </summary>
        /// <param name="characterService">The character service.</param>
        /// <param name="characterRoles">The character role service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="guildAPI">The guild API.</param>
        /// <param name="feedback">The feedback service.</param>
        public SetCommands
        (
            CharacterDiscordService characterService,
            CharacterRoleService characterRoles,
            ICommandContext context,
            IDiscordRestGuildAPI guildAPI,
            FeedbackService feedback
        )
        {
            _characters = characterService;
            _characterRoles = characterRoles;
            _context = context;
            _guildAPI = guildAPI;
            _feedback = feedback;
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
        public async Task<Result<FeedbackMessage>> SetCharacterNameAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
            Character character,
            string newCharacterName
        )
        {
            var setNameResult = await _characters.SetCharacterNameAsync(character, newCharacterName);
            return !setNameResult.IsSuccess
                ? Result<FeedbackMessage>.FromError(setNameResult)
                : new FeedbackMessage("Character name set.", _feedback.Theme.Secondary);
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
        public async Task<Result<FeedbackMessage>> SetCharacterAvatarAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
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
                if (!attachments.HasValue || attachments.Value.Count == 0)
                {
                    return new UserError("You need to attach an image or provide a url.");
                }

                var newAvatar = attachments.Value[0];
                newCharacterAvatarUrl = newAvatar.Url;
            }

            var galleryImage = character.Images.FirstOrDefault
            (
                i => i.Name.ToLower().Equals(newCharacterAvatarUrl.ToLower())
            );

            if (galleryImage is not null)
            {
                newCharacterAvatarUrl = galleryImage.Url;
            }

            var result = await _characters.SetCharacterAvatarAsync
            (
                character,
                newCharacterAvatarUrl ?? throw new ArgumentNullException(nameof(newCharacterAvatarUrl))
            );

            return !result.IsSuccess
                ? Result<FeedbackMessage>.FromError(result)
                : new FeedbackMessage("Character avatar set.", _feedback.Theme.Secondary);
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
        public async Task<Result<FeedbackMessage>> SetCharacterNicknameAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
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
                ? Result<FeedbackMessage>.FromError(setNickResult)
                : new FeedbackMessage("Character nickname set.", _feedback.Theme.Secondary);
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
        public async Task<Result<FeedbackMessage>> SetCharacterSummaryAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
            Character character,
            string newCharacterSummary
        )
        {
            var setSummaryResult = await _characters.SetCharacterSummaryAsync(character, newCharacterSummary);

            return !setSummaryResult.IsSuccess
                ? Result<FeedbackMessage>.FromError(setSummaryResult)
                : new FeedbackMessage("Character summary set.", _feedback.Theme.Secondary);
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
        public async Task<Result<FeedbackMessage>> SetCharacterDescriptionAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
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
                ? Result<FeedbackMessage>.FromError(setDescriptionResult)
                : new FeedbackMessage("Character description set.", _feedback.Theme.Secondary);
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
        public async Task<Result<FeedbackMessage>> SetCharacterIsNSFWAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
            Character character,
            bool isNSFW
        )
        {
            var setNSFW = await _characters.SetCharacterIsNSFWAsync(character, isNSFW);

            return !setNSFW.IsSuccess
                ? Result<FeedbackMessage>.FromError(setNSFW)
                : new FeedbackMessage($"Character set to {(isNSFW ? "NSFW" : "SFW")}.", _feedback.Theme.Secondary);
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
        public async Task<Result<FeedbackMessage>> SetCharacterPronounAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
            Character character,
            string pronounFamily
        )
        {
            var result = await _characters.SetCharacterPronounsAsync(character, pronounFamily);

            return !result.IsSuccess
                ? Result<FeedbackMessage>.FromError(result)
                : new FeedbackMessage("Preferred pronoun set.", _feedback.Theme.Secondary);
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
        public async Task<Result<FeedbackMessage>> SetCharacterRoleAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
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
                return Result<FeedbackMessage>.FromError(getRoleResult);
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
                return Result<FeedbackMessage>.FromError(getMember);
            }

            var member = getMember.Entity;

            var getGuildRoles = await _guildAPI.GetGuildRolesAsync(_context.GuildID.Value, this.CancellationToken);
            if (!getGuildRoles.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(getGuildRoles);
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
                ? Result<FeedbackMessage>.FromError(setRoleResult)
                : new FeedbackMessage("Character role set.", _feedback.Theme.Secondary);
        }

        /// <summary>
        /// Sets your default form to your current character.
        /// </summary>
        [UsedImplicitly]
        [Command("default-to-current")]
        [Description("Sets your default form to your current character.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<FeedbackMessage>> SetDefaultCharacterAsync()
        {
            var result = await _characters.GetCurrentCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            if (!result.IsSuccess)
            {
                return Result<FeedbackMessage>.FromError(result);
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
        public async Task<Result<FeedbackMessage>> SetDefaultCharacterAsync
        (
            [RequireEntityOwner]
            [AutocompleteProvider("character::owned")]
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
                ? Result<FeedbackMessage>.FromError(result)
                : new FeedbackMessage("Default character set.", _feedback.Theme.Secondary);
        }
    }
}
