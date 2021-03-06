﻿//
//  CharacterCommands.cs
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Feedback.Errors;
using DIGOS.Ambassador.Discord.Feedback.Results;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.Pagination.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Permissions;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Conditions;
using Humanizer;
using JetBrains.Annotations;
using Remora.Commands.Attributes;
using Remora.Commands.Groups;
using Remora.Discord.API;
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
    /// <summary>
    /// Commands for creating, editing, and interacting with user characters.
    /// </summary>
    [UsedImplicitly]
    [Group("ch")]
    [Description("Commands for creating, editing, and interacting with user characters.")]
    public partial class CharacterCommands : CommandGroup
    {
        private readonly PronounService _pronouns;
        private readonly ContentService _content;
        private readonly UserFeedbackService _feedback;
        private readonly CharacterDiscordService _characters;
        private readonly InteractivityService _interactivity;
        private readonly ICommandContext _context;
        private readonly IDiscordRestGuildAPI _guildAPI;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterCommands"/> class.
        /// </summary>
        /// <param name="contentService">The content service.</param>
        /// <param name="feedbackService">The feedback service.</param>
        /// <param name="characterService">The character service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="pronouns">The pronoun service.</param>
        /// <param name="context">The command context.</param>
        /// <param name="guildAPI">The guild API.</param>
        public CharacterCommands
        (
            ContentService contentService,
            UserFeedbackService feedbackService,
            CharacterDiscordService characterService,
            InteractivityService interactivity,
            PronounService pronouns,
            ICommandContext context,
            IDiscordRestGuildAPI guildAPI
        )
        {
            _content = contentService;
            _feedback = feedbackService;
            _characters = characterService;
            _interactivity = interactivity;
            _pronouns = pronouns;
            _context = context;
            _guildAPI = guildAPI;
        }

        /// <summary>
        /// Shows available pronoun families that can be used with characters.
        /// </summary>
        [UsedImplicitly]
        [Command("show-available-pronouns")]
        [Description("Shows available pronoun families that can be used with characters.")]
        public async Task<Result> ShowAvailablePronounFamiliesAsync()
        {
            EmbedField CreatePronounField(IPronounProvider pronounProvider)
            {
                var value = $"{pronounProvider.GetSubjectForm()} ate {pronounProvider.GetPossessiveAdjectiveForm()} " +
                            $"pie that {pronounProvider.GetSubjectForm()} brought with " +
                            $"{pronounProvider.GetReflexiveForm()}.";

                value = value.Transform(To.SentenceCase);

                return new EmbedField(pronounProvider.Family, value);
            }

            var pronounProviders = _pronouns.GetAvailablePronounProviders().ToList();
            if (!pronounProviders.Any())
            {
                return new UserError("There doesn't seem to be any pronouns available.");
            }

            var fields = pronounProviders.Select(CreatePronounField);
            var description = "Each field below represents a pronoun that can be used with a character. The title of " +
                              "each field is the pronoun family that you use when selecting the pronoun, and below it" +
                              "is a short example of how it might be used.";

            var pageBase = _feedback.CreateEmbedBase() with
            {
                Title = "Available pronouns"
            };

            var pages = PageFactory.FromFields
            (
                fields,
                description: description,
                pageBase: pageBase
            );

            await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );

            return Result.FromSuccess();
        }

        /// <summary>
        /// Shows quick information about your current character.
        /// </summary>
        [UsedImplicitly]
        [Command("show-current")]
        [Description("Shows quick information about your current character.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ShowCharacterAsync()
        {
            var retrieveCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            if (!retrieveCurrentCharacterResult.IsSuccess)
            {
                return Result.FromError(retrieveCurrentCharacterResult);
            }

            var character = retrieveCurrentCharacterResult.Entity;
            return await ShowCharacterAsync(character);
        }

        /// <summary>
        /// Shows quick information about a character.
        /// </summary>
        /// <param name="character">The character.</param>
        [UsedImplicitly]
        [Command("show")]
        [Description("Shows quick information about a character.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ShowCharacterAsync(Character character)
        {
            var createEmbed = await CreateCharacterInfoEmbedAsync(character);
            if (!createEmbed.IsSuccess)
            {
                return Result.FromError(createEmbed);
            }

            var send = await _feedback.SendContextualEmbedAsync(createEmbed.Entity, this.CancellationToken);
            return send.IsSuccess
                ? Result.FromSuccess()
                : Result.FromError(send);
        }

        /// <summary>
        /// Shows a gallery of all your characters.
        /// </summary>
        [UsedImplicitly]
        [Command("view-characters")]
        [Description("Shows a gallery of all your characters.")]
        [RequireContext(ChannelContext.Guild)]
        public Task<Result> ShowCharactersAsync() => ShowCharactersAsync(_context.User);

        /// <summary>
        /// Shows a gallery of all the user's characters.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        [UsedImplicitly]
        [Command("view-user-characters")]
        [Description("Shows a gallery of all the user's characters.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ShowCharactersAsync(IUser discordUser)
        {
            var getCharacters = await _characters.GetUserCharactersAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            if (!getCharacters.IsSuccess)
            {
                return Result.FromError(getCharacters);
            }

            var characters = getCharacters.Entity.ToList();

            var pages = await PaginatedEmbedFactory.PagesFromCollectionAsync
            (
                characters,
                async c =>
                {
                    var createEmbed = await CreateCharacterInfoEmbedAsync(c);
                    if (!createEmbed.IsSuccess)
                    {
                        return createEmbed;
                    }

                    return createEmbed.Entity with
                    {
                        Title = $"{(_context.User.ID == discordUser.ID ? "Your" : $"<@{discordUser.ID}>'s")} characters"
                    };
                },
                "You don't have any characters"
            );

            await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                _context.User.ID,
                pages.Where(p => p.IsSuccess).Select(p => p.Entity!).ToList(),
                ct: this.CancellationToken
            );

            return Result.FromSuccess();
        }

        private async Task<Result<Embed>> CreateCharacterInfoEmbedAsync
        (
            Character character,
            CancellationToken ct = default
        )
        {
            var eb = _feedback.CreateEmbedBase();

            var getOwner = await _guildAPI.GetGuildMemberAsync(_context.GuildID.Value, character.Owner.DiscordID, ct);
            if (!getOwner.IsSuccess)
            {
                return Result<Embed>.FromError(getOwner);
            }

            var owner = getOwner.Entity;
            var ownerName = owner.Nickname.HasValue && owner.Nickname.Value is not null
                ? owner.Nickname.Value
                : owner.User.Value!.Username;

            var author = new EmbedAuthor(ownerName);

            var getOwnerAvatar = CDN.GetUserAvatarUrl(owner.User.Value!);
            if (getOwnerAvatar.IsSuccess)
            {
                author = author with { IconUrl = getOwnerAvatar.Entity.ToString() };
            }

            var characterInfoTitle = character.Nickname.IsNullOrWhitespace()
                ? character.Name
                : $"{character.Name} - \"{character.Nickname}\"";

            var characterAvatarUrl = !character.AvatarUrl.IsNullOrWhitespace()
                ? character.AvatarUrl
                : _content.GetDefaultAvatarUri().ToString();

            var embedFields = new List<EmbedField>
            {
                new("Preferred pronouns", character.PronounProviderFamily)
            };

            if (character.Role is not null)
            {
                embedFields.Add(new EmbedField("Role", $"<@&{character.Role.DiscordID}>"));
            }

            eb = eb with
            {
                Title = characterInfoTitle,
                Description = character.Summary,
                Thumbnail = new EmbedThumbnail(characterAvatarUrl),
                Fields = embedFields,
                Author = author
            };

            if (character.Images.Any())
            {
                eb = eb with
                {
                    Footer = new EmbedFooter
                    (
                        $"This character has one or more images. Use \"!ch view-gallery {character.Name}\" to view " +
                        "them."
                    )
                };
            }

            // Override the colour if a role is set
            if (character.Role is not null)
            {
                var getGuildRoles = await _guildAPI.GetGuildRolesAsync(_context.GuildID.Value, this.CancellationToken);
                if (!getGuildRoles.IsSuccess)
                {
                    return Result<Embed>.FromError(getGuildRoles);
                }

                var guildRoles = getGuildRoles.Entity;
                var guildRole = guildRoles.FirstOrDefault(gr => gr.ID == character.Role.DiscordID);

                var roleColour = guildRole?.Colour ?? Color.Gray;

                eb = eb with { Colour = roleColour };
            }

            // Finally, the description
            embedFields.Add
            (
                eb.CalculateEmbedLength() + character.Description.Length > 2000
                    ? new EmbedField("Description", "Your description is really long, and can't be displayed.")
                    : new EmbedField("Description", character.Description)
            );

            return eb;
        }

        /// <summary>
        /// Creates a new character.
        /// </summary>
        /// <param name="characterName">The name of the character.</param>
        /// <param name="characterNickname">The nickname that the user should assume when the character is active. Optional.</param>
        /// <param name="characterSummary">A summary of the character. Optional. Max 240 characters.</param>
        /// <param name="characterDescription">The full description of the character. Optional.</param>
        /// <param name="characterAvatarUrl">A url pointing to the character's avatar. Optional.</param>
        [UsedImplicitly]
        [Command("create")]
        [Description("Creates a new character.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(CreateCharacter), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> CreateCharacterAsync
        (
            string characterName,
            string? characterNickname = null,
            string? characterSummary = null,
            string? characterDescription = null,
            string? characterAvatarUrl = null
        )
        {
            characterAvatarUrl ??= _content.GetDefaultAvatarUri().ToString();

            var createCharacterResult = await _characters.CreateCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                characterName,
                characterAvatarUrl,
                characterNickname,
                characterSummary,
                characterDescription,
                ct: this.CancellationToken
            );

            return !createCharacterResult.IsSuccess
                ? Result<UserMessage>.FromError(createCharacterResult)
                : new ConfirmationMessage($"Character \"{createCharacterResult.Entity.Name}\" created.");
        }

        /// <summary>
        /// Deletes the named character.
        /// </summary>
        /// <param name="character">The character to delete.</param>
        [UsedImplicitly]
        [Command("delete")]
        [Description("Deletes the named character.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(DeleteCharacter), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> DeleteCharacterAsync
        (
            [RequireEntityOwner]
            Character character
        )
        {
            var deleteResult = await _characters.DeleteCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                character,
                this.CancellationToken
            );

            return !deleteResult.IsSuccess
                ? Result<UserMessage>.FromError(deleteResult)
                : new ConfirmationMessage($"Character \"{character.Name}\" deleted.");
        }

        /// <summary>
        /// Lists the characters owned by a given user.
        /// </summary>
        /// <param name="discordUser">The user whose characters should be listed. Optional.</param>
        [UsedImplicitly]
        [Command("list")]
        [Description("Lists the characters owned by a given user.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ListOwnedCharactersAsync(IUser? discordUser = null)
        {
            discordUser ??= _context.User;

            var getCharacters = await _characters.GetUserCharactersAsync
            (
                _context.GuildID.Value,
                discordUser.ID,
                this.CancellationToken
            );

            if (!getCharacters.IsSuccess)
            {
                return Result.FromError(getCharacters);
            }

            var characters = getCharacters.Entity.ToList();

            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                characters,
                c => c.Name,
                c => c.Summary,
                "You don't have any characters."
            );

            pages = pages.Select(p => p with { Title = "Your characters" }).ToList();

            await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );

            return Result.FromSuccess();
        }

        /// <summary>
        /// Switches the user's current character to a different one, picked at random.
        /// </summary>
        [UsedImplicitly]
        [Command("random")]
        [Description("Switches the user's current character to a different one, picked at random.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> AssumeRandomCharacterFormAsync()
        {
            var getRandom = await _characters.GetRandomUserCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            if (!getRandom.IsSuccess)
            {
                return Result<UserMessage>.FromError(getRandom);
            }

            var randomCharacter = getRandom.Entity;
            return await AssumeCharacterFormAsync(randomCharacter);
        }

        /// <summary>
        /// Sets the named character as the user's current character.
        /// </summary>
        /// <param name="character">The character to become.</param>
        [UsedImplicitly]
        [Command("become")]
        [Description("Sets the named character as the user's current character.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(AssumeCharacter), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> AssumeCharacterFormAsync
        (
            [RequireEntityOwner]
            Character character
        )
        {
            var makeCurrent = await _characters.MakeCharacterCurrentAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                character,
                this.CancellationToken
            );

            if (!makeCurrent.IsSuccess)
            {
                return Result<UserMessage>.FromError(makeCurrent);
            }

            return new ConfirmationMessage
            (
                $"{_context.User.Username} shimmers and morphs into {character.Name}."
            );
        }

        /// <summary>
        /// Clears your default form.
        /// </summary>
        [UsedImplicitly]
        [Command("clear-default")]
        [Description("Clears your default form.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ClearDefaultCharacterAsync()
        {
            var result = await _characters.ClearDefaultCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            return !result.IsSuccess
                ? Result<UserMessage>.FromError(result)
                : new ConfirmationMessage("Default character cleared.");
        }

        /// <summary>
        /// Clears any active characters from you, restoring your default form.
        /// </summary>
        [UsedImplicitly]
        [Command("default")]
        [Description("Clears any active characters from you, restoring your default form.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result<UserMessage>> ClearCharacterFormAsync()
        {
            // First, let's try dropping to a default form instead.
            var getDefaultCharacter = await _characters.GetDefaultCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            if (getDefaultCharacter.IsSuccess)
            {
                var defaultCharacter = getDefaultCharacter.Entity;
                return await AssumeCharacterFormAsync(defaultCharacter);
            }

            var result = await _characters.ClearCurrentCharacterAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                this.CancellationToken
            );

            return !result.IsSuccess
                ? Result<UserMessage>.FromError(result)
                : new ConfirmationMessage("Character cleared.");
        }

        /// <summary>
        /// View the images in a character's gallery.
        /// </summary>
        /// <param name="character">The character to view the gallery of.</param>
        [UsedImplicitly]
        [Command("view-gallery")]
        [Description("View the images in a character's gallery.")]
        [RequireContext(ChannelContext.Guild)]
        public async Task<Result> ViewCharacterGalleryAsync(Character character)
        {
            if (character.Images.Count <= 0)
            {
                return new UserError("There are no images in that character's gallery.");
            }

            var appearance = PaginatedAppearanceOptions.Default with
            {
                FooterFormat = "Image {0}/{1}",
                HelpText = "Use the reactions to navigate the gallery."
            };

            var pages = character.Images.Select
            (
                i => new Embed(i.Name, Description: i.Caption, Image: new EmbedImage(i.Url))
            );

            return await _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                _context.User.ID,
                pages.ToList(),
                appearance,
                this.CancellationToken
            );
        }

        /// <summary>
        /// Lists the images in a character's gallery.
        /// </summary>
        /// <param name="character">The character.</param>
        [UsedImplicitly]
        [Command("list-images")]
        [Description("Lists the images in a character's gallery.")]
        [RequireContext(ChannelContext.Guild)]
        public Task<Result> ListImagesAsync(Character character)
        {
            var pages = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                character.Images,
                i => i.Name,
                i => i.Caption.IsNullOrWhitespace() ? "No caption set." : i.Caption,
                "There are no images in the character's gallery."
            );

            pages = pages.Select(p => p with { Title = "Images in character gallery" }).ToList();

            return _interactivity.SendInteractiveMessageAsync
            (
                _context.ChannelID,
                _context.User.ID,
                pages,
                ct: this.CancellationToken
            );
        }

        /// <summary>
        /// Adds an attached image to a character's gallery.
        /// </summary>
        /// <param name="character">The character to add the image to.</param>
        /// <param name="imageName">The name of the image to add.</param>
        /// <param name="imageCaption">The caption of the image.</param>
        /// <param name="isNSFW">Whether or not the image is NSFW.</param>
        [UsedImplicitly]
        [Command("add-image")]
        [Description("Adds an attached image to a character's gallery.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> AddImageAsync
        (
            [RequireEntityOwner]
            Character character,
            string? imageName = null,
            string? imageCaption = null,
            bool isNSFW = false
        )
        {
            if (_context is not MessageContext messageContext)
            {
                return new UserError("Images can't be added via slash commands. This is a discord limitation.");
            }

            var attachments = messageContext.Message.Attachments;
            if (!attachments.HasValue || attachments.Value.Count == 0)
            {
                return new UserError("You need to attach an image.");
            }

            // Check that it's an image
            var firstAttachment = attachments.Value[0];
            var firstAttachmentIsImage = firstAttachment.Width.HasValue && firstAttachment.Height.HasValue;

            if (!firstAttachmentIsImage)
            {
                return new UserError("You need to attach an image.");
            }

            var imageUrl = firstAttachment.Url;
            imageName ??= Path.GetFileNameWithoutExtension(firstAttachment.Filename);

            return await AddImageAsync(character, imageName, imageUrl, imageCaption, isNSFW);
        }

        /// <summary>
        /// Adds a linked image to a character's gallery.
        /// </summary>
        /// <param name="character">The character to add the image to.</param>
        /// <param name="imageName">The name of the image to add.</param>
        /// <param name="imageUrl">The url to the image.</param>
        /// <param name="imageCaption">The caption of the image.</param>
        /// <param name="isNSFW">Whether or not the image is NSFW.</param>
        [UsedImplicitly]
        [Command("add-linked-image")]
        [Description("Adds a linked image to a character's gallery.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> AddImageAsync
        (
            [RequireEntityOwner]
            Character character,
            string imageName,
            string imageUrl,
            string? imageCaption = null,
            bool isNSFW = false
        )
        {
            var addImageResult = await _characters.AddImageToCharacterAsync
            (
                character,
                imageName,
                imageUrl,
                imageCaption,
                isNSFW
            );

            return !addImageResult.IsSuccess
                ? Result<UserMessage>.FromError(addImageResult)
                : new ConfirmationMessage($"Added \"{imageName}\" to {character.Name}'s gallery.");
        }

        /// <summary>
        /// Removes the named image from the given character.
        /// </summary>
        /// <param name="character">The character to remove the image from.</param>
        /// <param name="imageName">The image to remove.</param>
        [UsedImplicitly]
        [Command("remove-image")]
        [Description("Removes an image from a character's gallery.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> RemoveImageAsync
        (
            [RequireEntityOwner]
            Character character,
            string imageName
        )
        {
            var image = character.Images.FirstOrDefault(i => string.Equals(imageName.ToLower(), i.Name.ToLower()));
            if (image is null)
            {
                return new UserError("The character doesn't have an image with that name.");
            }

            var removeImageResult = await _characters.RemoveImageFromCharacterAsync(character, image);

            return !removeImageResult.IsSuccess
                ? Result<UserMessage>.FromError(removeImageResult)
                : new ConfirmationMessage("Image removed.");
        }

        /// <summary>
        /// Transfers ownership of the named character to another user.
        /// </summary>
        /// <param name="newOwner">The new owner of the character.</param>
        /// <param name="character">The character to transfer.</param>
        [UsedImplicitly]
        [Command("transfer")]
        [Description("Transfers ownership of the named character to another user.")]
        [RequireContext(ChannelContext.Guild)]
        [RequirePermission(typeof(TransferCharacter), PermissionTarget.Self)]
        public async Task<Result<UserMessage>> TransferCharacterOwnershipAsync
        (
            IUser newOwner,
            [RequireEntityOwner]
            Character character
        )
        {
            var transferResult = await _characters.TransferCharacterOwnershipAsync
            (
                _context.GuildID.Value,
                _context.User.ID,
                character,
                this.CancellationToken
            );

            return !transferResult.IsSuccess
                ? Result<UserMessage>.FromError(transferResult)
                : new ConfirmationMessage("Character ownership transferred.");
        }
    }
}
