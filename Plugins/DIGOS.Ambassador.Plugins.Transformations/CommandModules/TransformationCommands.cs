﻿//
//  TransformationCommands.cs
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
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API.Abstractions.Objects;
using Remora.Discord.API.Objects;
using Remora.Discord.Commands.Conditions;
using Remora.Discord.Commands.Contexts;
using Remora.Results;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Transformations.CommandModules
{
    /// <summary>
    /// Transformation-related commands, such as transforming certain body parts or saving transforms as characters.
    /// </summary>
    [Group("tf")]
    [Description("Transformation-related commands, such as transforming body parts or saving appearances.")]
    public partial class TransformationCommands : CommandGroup
    {
        private readonly UserFeedbackService _feedback;
        private readonly ContentService _content;
        private readonly CharacterDiscordService _characters;
        private readonly TransformationService _transformation;
        private readonly ICommandContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationCommands"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="characters">The character service.</param>
        /// <param name="transformation">The transformation service.</param>
        /// <param name="content">The content service.</param>
        /// <param name="context">The command context.</param>
        public TransformationCommands
        (
            UserFeedbackService feedback,
            CharacterDiscordService characters,
            TransformationService transformation,
            ContentService content,
            ICommandContext context
        )
        {
            _feedback = feedback;
            _characters = characters;
            _transformation = transformation;
            _content = content;
            _context = context;
        }

        /// <summary>
        /// Transforms the given bodypart into the given species on the target user.
        /// </summary>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="species">The species to transform it into.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="target">The user to transform.</param>
        [UsedImplicitly]
        [Command("species")]
        [Description("Transforms the given bodypart of the target user into the given species.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ShiftAsync
        (
            Bodypart bodyPart,
            string species,
            Chirality chirality = Chirality.Center,
            IUser? target = null
        )
        {
            target ??= _context.User;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                _context.GuildID.Value,
                target.ID
            );

            if (!getCurrentCharacterResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getCurrentCharacterResult);
            }

            var character = getCurrentCharacterResult.Entity;

            Result<ShiftBodypartResult> shift;
            if (species.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                shift = await _transformation.RemoveBodypartAsync(_context.User.ID, character, bodyPart, chirality);
            }
            else
            {
                shift = await _transformation.ShiftBodypartAsync
                (
                    _context.User.ID,
                    character,
                    bodyPart,
                    species,
                    chirality
                );
            }

            if (!shift.IsSuccess)
            {
                return Result<UserMessage>.FromError(shift);
            }

            var result = shift.Entity;

            return new ConfirmationMessage(result.ShiftMessage);
        }

        /// <summary>
        /// Transforms the base colour of the given bodypart on the target user into the given colour.
        /// </summary>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="target">The target user.</param>
        [UsedImplicitly]
        [Description("Transforms the base colour of the given bodypart on the target user into the given colour.")]
        [Command("colour")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ShiftColourAsync
        (
            Bodypart bodyPart,
            Colour colour,
            Chirality chirality = Chirality.Center,
            IUser? target = null
        )
        {
            target ??= _context.User;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                _context.GuildID.Value,
                target.ID
            );

            if (!getCurrentCharacterResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getCurrentCharacterResult);
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await _transformation.ShiftBodypartColourAsync
            (
                _context.User.ID,
                character,
                bodyPart,
                colour,
                chirality
            );

            return !shiftPartResult.IsSuccess
                ? Result<UserMessage>.FromError(shiftPartResult)
                : new ConfirmationMessage(shiftPartResult.Entity.ShiftMessage);
        }

        /// <summary>
        /// Transforms the pattern on the given bodypart on the target user into the given pattern and secondary colour.
        /// </summary>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="pattern">The pattern to transform it into.</param>
        /// <param name="colour">The colour to transform it into.</param>
        /// <param name="chirality">The chirality of the part.</param>
        /// <param name="target">The target user.</param>
        [UsedImplicitly]
        [Command("pattern")]
        [Description("Transforms the pattern on the given bodypart on the target user into the given pattern and colour.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ShiftPatternAsync
        (
            Bodypart bodyPart,
            Pattern pattern,
            Colour colour,
            Chirality chirality = Chirality.Center,
            IUser? target = null
        )
        {
            target ??= _context.User;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                _context.GuildID.Value,
                target.ID
            );

            if (!getCurrentCharacterResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getCurrentCharacterResult);
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await _transformation.ShiftBodypartPatternAsync
            (
                _context.User.ID,
                character,
                bodyPart,
                pattern,
                colour,
                chirality
            );

            return !shiftPartResult.IsSuccess
                ? Result<UserMessage>.FromError(shiftPartResult)
                : new ConfirmationMessage(shiftPartResult.Entity.ShiftMessage);
        }

        /// <summary>
        /// Transforms the colour of the pattern on the given bodypart on the target user to the given colour.
        /// </summary>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        /// <param name="chirality">The chirality of the part.</param>
        /// <param name="target">The target user.</param>
        [UsedImplicitly]
        [Command("pattern-colour")]
        [Description("Transforms the colour of the pattern on the given bodypart on the target user to the given colour.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ShiftPatternColourAsync
        (
            Bodypart bodyPart,
            Colour colour,
            Chirality chirality = Chirality.Center,
            IUser? target = null
        )
        {
            target ??= _context.User;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                _context.GuildID.Value,
                target.ID
            );

            if (!getCurrentCharacterResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getCurrentCharacterResult);
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await _transformation.ShiftPatternColourAsync
            (
                _context.User.ID,
                character,
                bodyPart,
                colour,
                chirality
            );

            return !shiftPartResult.IsSuccess
                ? Result<UserMessage>.FromError(shiftPartResult)
                : new ConfirmationMessage(shiftPartResult.Entity.ShiftMessage);
        }

        /// <summary>
        /// Describes the current physical appearance of a character.
        /// </summary>
        /// <param name="character">The character to describe.</param>
        [UsedImplicitly]
        [Command("describe")]
        [Description("Describes the current physical appearance of a character.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> DescribeCharacterAsync(Character? character = null)
        {
            if (character is null)
            {
                var getCurrent = await _characters.GetCurrentCharacterAsync(_context.GuildID.Value, _context.User.ID);
                if (!getCurrent.IsSuccess)
                {
                    return Result.FromError(getCurrent);
                }

                character = getCurrent.Entity;
            }

            var generateDescriptionAsync = await _transformation.GenerateCharacterDescriptionAsync
            (
                character
            );

            if (!generateDescriptionAsync.IsSuccess)
            {
                return Result.FromError(generateDescriptionAsync);
            }

            var descriptionEmbed = _feedback.CreateEmbedBase() with
            {
                Description = generateDescriptionAsync.Entity
            };

            var characterEmbed = _feedback.CreateEmbedBase() with
            {
                Title = $"{character.Name} \"{character.Nickname}\"".Trim(),
                Thumbnail = new EmbedThumbnail
                (
                    !character.AvatarUrl.IsNullOrWhitespace()
                        ? character.AvatarUrl
                        : _content.GetDefaultAvatarUri().ToString()
                ),
                Fields = new[] { new EmbedField("Description", character.Description) }
            };

            var sendCharacter = await _feedback.SendPrivateEmbedAsync(_context.User.ID, characterEmbed);
            if (!sendCharacter.IsSuccess)
            {
                return Result.FromError(sendCharacter);
            }

            var sendDescription = await _feedback.SendPrivateEmbedAsync
            (
                _context.User.ID,
                descriptionEmbed
            );

            return sendDescription.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(sendDescription);
        }

        /// <summary>
        /// Resets your form to your default one.
        /// </summary>
        [UsedImplicitly]
        [Command("reset")]
        [Description("Resets your form to your default one.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ResetFormAsync()
        {
            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID
            );

            if (!getCurrentCharacterResult.IsSuccess)
            {
                return Result<UserMessage>.FromError(getCurrentCharacterResult);
            }

            var character = getCurrentCharacterResult.Entity;
            var resetFormResult = await _transformation.ResetCharacterFormAsync(character);

            return !resetFormResult.IsSuccess
                ? Result<UserMessage>.FromError(resetFormResult)
                : new ConfirmationMessage("Character form reset.");
        }

        /// <summary>
        /// Opts into the transformation module on this server.
        /// </summary>
        [UsedImplicitly]
        [Command("opt-in")]
        [Description("Opts into the transformation module on this server.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> OptInToTransformationsAsync()
        {
            var optInResult = await _transformation.OptInUserAsync(_context.User.ID, _context.GuildID.Value);

            return !optInResult.IsSuccess
                ? Result<UserMessage>.FromError(optInResult)
                : new ConfirmationMessage("Opted into transformations. Have fun!");
        }

        /// <summary>
        /// Opts into the transformation module on this server.
        /// </summary>
        [UsedImplicitly]
        [Command("opt-out")]
        [Description("Opts out of the transformation module on this server.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> OptOutOfTransformationsAsync()
        {
            var optOutResult = await _transformation.OptOutUserAsync(_context.User.ID, _context.GuildID.Value);

            return !optOutResult.IsSuccess
                ? Result<UserMessage>.FromError(optOutResult)
                : new ConfirmationMessage("Opted out of transformations.");
        }

        /// <summary>
        /// Whitelists a user, allowing them to transform you.
        /// </summary>
        /// <param name="user">The user to whitelist.</param>
        [UsedImplicitly]
        [Command("whitelist")]
        [Description("Whitelists a user, allowing them to transform you.")]
        public async Task<Result<UserMessage>> WhitelistUserAsync(IUser user)
        {
            var whitelistUserResult = await _transformation.WhitelistUserAsync(_context.User.ID, user.ID);
            return !whitelistUserResult.IsSuccess
                ? Result<UserMessage>.FromError(whitelistUserResult)
                : new ConfirmationMessage("User whitelisted.");
        }

        /// <summary>
        /// Blacklists a user, preventing them from transforming you.
        /// </summary>
        /// <param name="user">The user to blacklist.</param>
        [UsedImplicitly]
        [Command("blacklist")]
        [Description("Blacklists a user, preventing them from transforming you.")]
        public async Task<Result<UserMessage>> BlacklistUserAsync(IUser user)
        {
            var blacklistUserResult = await _transformation.BlacklistUserAsync(_context.User.ID, user.ID);

            return !blacklistUserResult.IsSuccess
                ? Result<UserMessage>.FromError(blacklistUserResult)
                : new ConfirmationMessage("User whitelisted.");
        }

        /// <summary>
        /// Updates the transformation database with the bundled definitions.
        /// </summary>
        [UsedImplicitly]
        [Command("update-db")]
        [Description("Updates the transformation database with the bundled definitions.")]
        [RequireOwner]
        public async Task<Result<UserMessage>> UpdateTransformationDatabaseAsync()
        {
            var updateTransformations = await _transformation.UpdateTransformationDatabaseAsync();
            if (!updateTransformations.IsSuccess)
            {
                return Result<UserMessage>.FromError(updateTransformations);
            }

            var result = updateTransformations.Entity;

            var confirmationText =
                $" Database updated. {result.SpeciesAdded} species added, " +
                $"{result.TransformationsAdded} transformations added, " +
                $"{result.SpeciesUpdated} species updated, " +
                $"and {result.TransformationsUpdated} transformations updated.";

            return new ConfirmationMessage(confirmationText);
        }
    }
}
