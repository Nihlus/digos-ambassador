//
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Extensions;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Pagination;
using DIGOS.Ambassador.Plugins.Characters.Permissions;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Characters.Services.Pronouns;
using DIGOS.Ambassador.Plugins.Core.Preconditions;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Image = DIGOS.Ambassador.Plugins.Characters.Model.Data.Image;
using PermissionTarget = DIGOS.Ambassador.Plugins.Permissions.Model.PermissionTarget;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Characters.CommandModules
{
    /// <summary>
    /// Commands for creating, editing, and interacting with user characters.
    /// </summary>
    [UsedImplicitly]
    [Alias("character", "char", "ch")]
    [Group("character")]
    [Summary("Commands for creating, editing, and interacting with user characters.")]
    [Remarks
    (
        "Parameters which take a character can be specified in two ways - by just the name, which will search your " +
        "characters, and by mention and name, which will search the given user's characters. For example,\n" +
        "\n" +
        "Your character: Amby  \n" +
        "Another user's character: @DIGOS Ambassador:Amby\n" +
        "\n" +
        "You can also substitute any character name for \"current\", and your active character will be used instead."
    )]
    public partial class CharacterCommands : ModuleBase
    {
        private readonly PronounService _pronouns;
        private readonly ContentService _content;
        private readonly UserFeedbackService _feedback;
        private readonly CharacterDiscordService _characters;
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterCommands"/> class.
        /// </summary>
        /// <param name="contentService">The content service.</param>
        /// <param name="feedbackService">The feedback service.</param>
        /// <param name="characterService">The character service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="pronouns">The pronoun service.</param>
        public CharacterCommands
        (
            ContentService contentService,
            UserFeedbackService feedbackService,
            CharacterDiscordService characterService,
            InteractivityService interactivity,
            PronounService pronouns
        )
        {
            _content = contentService;
            _feedback = feedbackService;
            _characters = characterService;
            _interactivity = interactivity;
            _pronouns = pronouns;
        }

        /// <summary>
        /// Shows available pronoun families that can be used with characters.
        /// </summary>
        [UsedImplicitly]
        [Alias("available-pronouns", "pronouns")]
        [Command("available-pronouns")]
        [Summary("Shows available pronoun families that can be used with characters.")]
        public async Task ShowAvailablePronounFamiliesAsync()
        {
            EmbedFieldBuilder CreatePronounField(IPronounProvider pronounProvider)
            {
                var ef = new EmbedFieldBuilder();

                ef.WithName(pronounProvider.Family);

                var value = $"{pronounProvider.GetSubjectForm()} ate {pronounProvider.GetPossessiveAdjectiveForm()} " +
                            $"pie that {pronounProvider.GetSubjectForm()} brought with " +
                            $"{pronounProvider.GetReflexiveForm()}.";

                value = value.Transform(To.SentenceCase);

                ef.WithValue($"*{value}*");

                return ef;
            }

            var pronounProviders = _pronouns.GetAvailablePronounProviders().ToList();
            if (!pronounProviders.Any())
            {
                await _feedback.SendErrorAsync(this.Context, "There doesn't seem to be any pronouns available.");
                return;
            }

            var fields = pronounProviders.Select(CreatePronounField);
            var description = "Each field below represents a pronoun that can be used with a character. The title of " +
                              "each field is the pronoun family that you use when selecting the pronoun, and below it" +
                              "is a short example of how it might be used.";

            var paginatedEmbedPages = PageFactory.FromFields
            (
                fields,
                description: description
            );

            var paginatedEmbed = new PaginatedEmbed(_feedback, _interactivity, this.Context.User).WithPages
            (
                paginatedEmbedPages.Select
                (
                    p => p.WithColor(Color.DarkPurple).WithTitle("Available pronouns")
                )
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );
        }

        /// <summary>
        /// Shows quick information about your current character.
        /// </summary>
        [UsedImplicitly]
        [Alias("show", "info")]
        [Command("show")]
        [Summary("Shows quick information about your current character.")]
        [RequireContext(ContextType.Guild)]
        public async Task ShowCharacterAsync()
        {
            var retrieveCurrentCharacterResult = await _characters.GetCurrentCharacterAsync((IGuildUser)this.Context.User);
            if (!retrieveCurrentCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, retrieveCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = retrieveCurrentCharacterResult.Entity;
            var eb = await CreateCharacterInfoEmbedAsync(character);

            // Override the colour if a role is set.
            if (!(character.Role is null))
            {
                var roleColour = this.Context.Guild.GetRole((ulong)character.Role.DiscordID).Color;
                eb.WithColor(roleColour);
            }

            await ShowCharacterAsync(character, eb);
        }

        /// <summary>
        /// Shows quick information about a character.
        /// </summary>
        /// <param name="character">The character.</param>
        [UsedImplicitly]
        [Alias("show", "info")]
        [Priority(1)]
        [Command("show")]
        [Summary("Shows quick information about a character.")]
        [RequireContext(ContextType.Guild)]
        public async Task ShowCharacterAsync(Character character)
        {
            var eb = await CreateCharacterInfoEmbedAsync(character);
            await ShowCharacterAsync(character, eb);
        }

        /// <summary>
        /// Shows a gallery of all your characters.
        /// </summary>
        [UsedImplicitly]
        [Alias("view-char")]
        [Command("view-characters")]
        [Summary("Shows a gallery of all your characters.")]
        [RequireContext(ContextType.Guild)]
        public Task ShowCharactersAsync() => ShowCharactersAsync(this.Context.User);

        /// <summary>
        /// Shows a gallery of all the user's characters.
        /// </summary>
        /// <param name="discordUser">The user.</param>
        [UsedImplicitly]
        [Alias("view-char")]
        [Command("view-characters")]
        [Summary("Shows a gallery of all the user's characters.")]
        [RequireContext(ContextType.Guild)]
        public async Task ShowCharactersAsync(IUser discordUser)
        {
            var getCharacters = await _characters.GetUserCharactersAsync((IGuildUser)this.Context.User);
            if (!getCharacters.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCharacters.ErrorReason);
                return;
            }

            var characters = await getCharacters.Entity.ToListAsync();

            var embeds = new List<EmbedBuilder>();
            foreach (var character in characters)
            {
                var embed = await CreateCharacterInfoEmbedAsync(character);
                if (character.Description.Length + embed.Build().Length < 2000)
                {
                    embed.AddField("Description", character.Description);
                }

                embeds.Add(embed);
            }

            var paginatedEmbed = new PaginatedEmbed(_feedback, _interactivity, this.Context.User)
            {
                Appearance =
                {
                    Author = discordUser,
                    Title =
                        $"{(this.Context.User == discordUser ? "Your" : $"{discordUser.Mention}'s")} characters"
                }
            };

            if (embeds.Count == 0)
            {
                var eb = paginatedEmbed.Appearance.CreateEmbedBase().WithDescription("You don't have any characters.");
                paginatedEmbed.AppendPage(eb);
            }
            else
            {
                paginatedEmbed.WithPages(embeds);
            }

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );
        }

        private async Task ShowCharacterAsync(Character character, EmbedBuilder eb)
        {
            if (character.Description.Length + eb.Build().Length > 2000)
            {
                var userDMChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();

                try
                {
                    await using (var ds = new MemoryStream(Encoding.UTF8.GetBytes(character.Description)))
                    {
                        await userDMChannel.SendMessageAsync(string.Empty, false, eb.Build());
                        await userDMChannel.SendFileAsync(ds, $"{character.Name}_description.txt");
                    }

                    if (this.Context is SocketCommandContext socketContext && socketContext.IsPrivate)
                    {
                        await _feedback.SendConfirmationAsync(this.Context, "Please check your private messages.");
                    }
                }
                catch (HttpException hex) when (hex.WasCausedByDMsNotAccepted())
                {
                    await _feedback.SendWarningAsync
                    (
                        this.Context,
                        "Your description is really long, and you don't accept DMs from non-friends on this server, so I'm unable to do that."
                    );
                }
                finally
                {
                    await userDMChannel.CloseAsync();
                }
            }
            else
            {
                eb.AddField("Description", character.Description);
                await _feedback.SendEmbedAsync(this.Context.Channel, eb.Build());
            }
        }

        private async Task<EmbedBuilder> CreateCharacterInfoEmbedAsync(Character character)
        {
            var eb = _feedback.CreateEmbedBase();

            // Override the colour if a role is set
            if (!(character.Role is null))
            {
                var roleColour = this.Context.Guild.GetRole((ulong)character.Role.DiscordID).Color;
                eb.WithColor(roleColour);
            }

            eb.WithAuthor(await this.Context.Client.GetUserAsync((ulong)character.Owner.DiscordID));

            var characterInfoTitle = character.Nickname.IsNullOrWhitespace()
                ? character.Name
                : $"{character.Name} - \"{character.Nickname}\"";

            eb.WithTitle(characterInfoTitle);
            eb.WithDescription(character.Summary);

            eb.WithThumbnailUrl
            (
                !character.AvatarUrl.IsNullOrWhitespace()
                    ? character.AvatarUrl
                    : _content.GetDefaultAvatarUri().ToString()
            );

            eb.AddField("Preferred pronouns", character.PronounProviderFamily);

            if (!(character.Role is null))
            {
               eb.AddField("Role", $"<@&{character.Role.DiscordID}>");
            }

            if (character.Images.Any())
            {
                eb.WithFooter
                (
                    $"This character has one or more images. Use \"!ch view-gallery {character.Name}\" to view them."
                );
            }

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
        [Summary("Creates a new character.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(CreateCharacter), PermissionTarget.Self)]
        public async Task CreateCharacterAsync
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
                (IGuildUser)this.Context.User,
                characterName,
                characterAvatarUrl,
                characterNickname,
                characterSummary,
                characterDescription
            );

            if (!createCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, createCharacterResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync
            (
                this.Context, $"Character \"{createCharacterResult.Entity.Name}\" created."
            );
        }

        /// <summary>
        /// Deletes the named character.
        /// </summary>
        /// <param name="character">The character to delete.</param>
        [UsedImplicitly]
        [Command("delete")]
        [Summary("Deletes the named character.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(DeleteCharacter), PermissionTarget.Self)]
        public async Task DeleteCharacterAsync
        (
            [RequireEntityOwnerOrPermission(typeof(DeleteCharacter), PermissionTarget.Other)]
            Character character
        )
        {
            var owner = await this.Context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
            if (owner is null)
            {
                await _feedback.SendErrorAsync(this.Context, "Failed to get the owner of the character.");
                return;
            }

            var deleteResult = await _characters.DeleteCharacterAsync(owner, character);
            if (!deleteResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, deleteResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, $"Character \"{character.Name}\" deleted.");
        }

        /// <summary>
        /// Lists the characters owned by a given user.
        /// </summary>
        /// <param name="discordUser">The user whose characters should be listed. Optional.</param>
        [UsedImplicitly]
        [Alias("list-owned", "list", "owned")]
        [Command("list-owned")]
        [Summary("Lists the characters owned by a given user.")]
        [RequireContext(ContextType.Guild)]
        public async Task ListOwnedCharactersAsync(IUser? discordUser = null)
        {
            discordUser ??= this.Context.Message.Author;

            var getCharacters = await _characters.GetUserCharactersAsync((IGuildUser)this.Context.User);
            if (!getCharacters.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCharacters.ErrorReason);
                return;
            }

            var characters = await getCharacters.Entity.ToListAsync();

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Your characters";
            appearance.Author = discordUser;

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                _interactivity,
                this.Context.User,
                characters,
                c => c.Name,
                c => c.Summary,
                "You don't have any characters.",
                appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );
        }

        /// <summary>
        /// Switches the user's current character to a different one, picked at random.
        /// </summary>
        [UsedImplicitly]
        [Alias("random")]
        [Command("random")]
        [Summary("Switches the user's current character to a different one, picked at random.")]
        [RequireContext(ContextType.Guild)]
        public async Task AssumeRandomCharacterFormAsync()
        {
            var getRandom = await _characters.GetRandomUserCharacterAsync((IGuildUser)this.Context.User);
            if (!getRandom.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getRandom.ErrorReason);
                return;
            }

            var randomCharacter = getRandom.Entity;
            await AssumeCharacterFormAsync(randomCharacter);
        }

        /// <summary>
        /// Sets the named character as the user's current character.
        /// </summary>
        /// <param name="character">The character to become.</param>
        [UsedImplicitly]
        [Alias("assume", "become", "transform", "active")]
        [Command("assume")]
        [Summary("Sets the named character as the user's current character.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(AssumeCharacter), PermissionTarget.Self)]
        public async Task AssumeCharacterFormAsync
        (
            [RequireEntityOwnerOrPermission(typeof(AssumeCharacter), PermissionTarget.Other)]
            Character character
        )
        {
            var makeCurrent = await _characters.MakeCharacterCurrentAsync((IGuildUser)this.Context.User, character);
            if (!makeCurrent.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, makeCurrent.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync
            (
                this.Context,
                $"{this.Context.Message.Author.Username} shimmers and morphs into {character.Name}."
            );
        }

        /// <summary>
        /// Clears your default form.
        /// </summary>
        [UsedImplicitly]
        [Alias("clear-default", "drop-default")]
        [Command("clear-default")]
        [Summary("Clears your default form.")]
        [RequireContext(ContextType.Guild)]
        public async Task ClearDefaultCharacterAsync()
        {
            var result = await _characters.ClearDefaultCharacterAsync((IGuildUser)this.Context.User);
            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Default character cleared.");
        }

        /// <summary>
        /// Clears any active characters from you, restoring your default form.
        /// </summary>
        [UsedImplicitly]
        [Alias("clear", "drop", "default")]
        [Command("clear")]
        [Summary("Clears any active characters from you, restoring your default form.")]
        [RequireContext(ContextType.Guild)]
        public async Task ClearCharacterFormAsync()
        {
            var result = await _characters.ClearCurrentCharacterAsync((IGuildUser)this.Context.User);
            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Character cleared.");
        }

        /// <summary>
        /// View the images in a character's gallery.
        /// </summary>
        /// <param name="character">The character to view the gallery of.</param>
        [UsedImplicitly]
        [Alias("view-gallery", "gallery")]
        [Command("view-gallery")]
        [Summary("View the images in a character's gallery.")]
        [RequireContext(ContextType.Guild)]
        public async Task ViewCharacterGalleryAsync(Character character)
        {
            if (character.Images.Count <= 0)
            {
                await _feedback.SendErrorAsync(this.Context, "There are no images in that character's gallery.");
                return;
            }

            var gallery = new PaginatedGallery(_feedback, _interactivity, this.Context.User)
                .WithPages(character.Images);

            gallery.Appearance = new PaginatedAppearanceOptions
            {
                FooterFormat = "Image {0}/{1}",
                HelpText = "Use the reactions to navigate the gallery.",
                Color = Color.DarkPurple,
                Title = character.Name
            };

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                gallery,
                TimeSpan.FromMinutes(5.0)
            );
        }

        /// <summary>
        /// Lists the images in a character's gallery.
        /// </summary>
        /// <param name="character">The character.</param>
        [UsedImplicitly]
        [Command("list-images")]
        [Summary("Lists the images in a character's gallery.")]
        [RequireContext(ContextType.Guild)]
        public async Task ListImagesAsync(Character character)
        {
            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Images in character gallery";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                _interactivity,
                this.Context.User,
                character.Images,
                i => i.Name,
                i => i.Caption.IsNullOrWhitespace() ? "No caption set." : i.Caption,
                "There are no images in the character's gallery.",
                appearance
            );

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
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
        [Summary("Adds an attached image to a character's gallery.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
        public async Task AddImageAsync
        (
            [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
            Character character,
            string? imageName = null,
            string? imageCaption = null,
            bool isNSFW = false
        )
        {
            var hasAtLeastOneAttachment = this.Context.Message.Attachments.Any();
            if (!hasAtLeastOneAttachment)
            {
                await _feedback.SendErrorAsync(this.Context, "You need to attach an image.");
                return;
            }

            // Check that it's an image
            var firstAttachment = this.Context.Message.Attachments.First();
            var firstAttachmentIsImage = firstAttachment.Width.HasValue && firstAttachment.Height.HasValue;

            if (!firstAttachmentIsImage)
            {
                await _feedback.SendErrorAsync(this.Context, "You need to attach an image.");
                return;
            }
            var imageUrl = firstAttachment.Url;

            imageName = (imageName ?? Path.GetFileNameWithoutExtension(firstAttachment.Filename))
                        ?? firstAttachment.Url.GetHashCode().ToString();

            await AddImageAsync(character, imageName, imageUrl, imageCaption, isNSFW);
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
        [Command("add-image")]
        [Summary("Adds a linked image to a character's gallery.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
        public async Task AddImageAsync
        (
            [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
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

            if (!addImageResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, addImageResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, $"Added \"{imageName}\" to {character.Name}'s gallery.");
        }

        /// <summary>
        /// Removes the named image from the given character.
        /// </summary>
        /// <param name="character">The character to remove the image from.</param>
        /// <param name="imageName">The image to remove.</param>
        [UsedImplicitly]
        [Alias("remove-image", "delete-image")]
        [Command("remove-image")]
        [Summary("Removes an image from a character's gallery.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
        public async Task RemoveImageAsync
        (
            [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
            Character character,
            string imageName
        )
        {
            var image = character.Images.FirstOrDefault(i => string.Equals(imageName.ToLower(), i.Name.ToLower()));
            if (image is null)
            {
                await _feedback.SendErrorAsync(this.Context, "The character doesn't have an image with that name.");
                return;
            }

            var removeImageResult = await _characters.RemoveImageFromCharacterAsync(character, image);
            if (!removeImageResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, removeImageResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Image removed.");
        }

        /// <summary>
        /// Transfers ownership of the named character to another user.
        /// </summary>
        /// <param name="newOwner">The new owner of the character.</param>
        /// <param name="character">The character to transfer.</param>
        [UsedImplicitly]
        [Alias("transfer-ownership", "transfer")]
        [Command("transfer-ownership")]
        [Summary("Transfers ownership of the named character to another user.")]
        [RequireContext(ContextType.Guild)]
        [RequirePermission(typeof(TransferCharacter), PermissionTarget.Self)]
        public async Task TransferCharacterOwnershipAsync
        (
            IUser newOwner,
            [RequireEntityOwnerOrPermission(typeof(TransferCharacter), PermissionTarget.Other)]
            Character character
        )
        {
            var transferResult = await _characters.TransferCharacterOwnershipAsync
            (
                (IGuildUser)this.Context.User, character
            );

            if (!transferResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, transferResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Character ownership transferred.");
        }
    }
}
