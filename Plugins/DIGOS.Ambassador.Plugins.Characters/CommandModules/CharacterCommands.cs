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
using DIGOS.Ambassador.Discord;
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
using DIGOS.Ambassador.Plugins.Core.Services.Servers;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Permissions.Preconditions;
using Discord;
using Discord.Commands;
using Discord.Net;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore;
using Remora.Results;
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
    public class CharacterCommands : ModuleBase
    {
        private readonly PronounService _pronouns;
        private readonly ServerService _servers;
        private readonly UserService _users;
        private readonly DiscordService _discord;
        private readonly ContentService _content;
        private readonly UserFeedbackService _feedback;
        private readonly CharacterService _characters;
        private readonly InteractivityService _interactivity;
        private readonly Random _random;

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterCommands"/> class.
        /// </summary>
        /// <param name="contentService">The content service.</param>
        /// <param name="discordService">The Discord integration service.</param>
        /// <param name="feedbackService">The feedback service.</param>
        /// <param name="characterService">The character service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="random">A cached, application-level entropy source.</param>
        /// <param name="users">The user service.</param>
        /// <param name="servers">The server service.</param>
        /// <param name="pronouns">The pronoun service.</param>
        public CharacterCommands
        (
            ContentService contentService,
            DiscordService discordService,
            UserFeedbackService feedbackService,
            CharacterService characterService,
            InteractivityService interactivity,
            Random random,
            UserService users,
            ServerService servers,
            PronounService pronouns
        )
        {
            _content = contentService;
            _discord = discordService;
            _feedback = feedbackService;
            _characters = characterService;
            _interactivity = interactivity;
            _random = random;
            _users = users;
            _servers = servers;
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
            var getInvokerResult = await _users.GetOrRegisterUserAsync(this.Context.User);
            if (!getInvokerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getInvokerResult.ErrorReason);
                return;
            }

            var invoker = getInvokerResult.Entity;

            var retrieveCurrentCharacterResult = await _characters.GetCurrentCharacterAsync(this.Context, invoker);
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
            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;
            var characters = await _characters.GetUserCharacters(user, this.Context.Guild).ToListAsync();

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
                this.Context,
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
            var deleteResult = await _characters.DeleteCharacterAsync(character);
            if (!deleteResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, deleteResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, $"Character \"{character.Name}\" deleted.");

            if (character.IsCurrent)
            {
                var owner = await this.Context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);

                if (!(character.Role is null))
                {
                    var newDiscordRole = this.Context.Guild.GetRole((ulong)character.Role.DiscordID);
                    var removeRoleResult = await _discord.RemoveUserRoleAsync
                    (
                        this.Context,
                        owner,
                        newDiscordRole
                    );

                    if (!removeRoleResult.IsSuccess)
                    {
                        await _feedback.SendErrorAsync(this.Context, removeRoleResult.ErrorReason);
                    }
                }

                var resetNickResult = await _discord.SetUserNicknameAsync
                (
                    this.Context,
                    owner,
                    owner.Username
                );

                if (!resetNickResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, resetNickResult.ErrorReason);
                }
            }
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

            var getUserResult = await _users.GetOrRegisterUserAsync(discordUser);
            if (!getUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;

            var characters = await _characters.GetUserCharacters(user, this.Context.Guild).ToListAsync();

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
            var getInvokerResult = await _users.GetOrRegisterUserAsync(this.Context.User);
            if (!getInvokerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getInvokerResult.ErrorReason);
                return;
            }

            var invoker = getInvokerResult.Entity;

            var userCharacters = await _characters.GetUserCharacters
            (
                invoker,
                this.Context.Guild
            ).ToListAsync();

            if (userCharacters.Count <= 0)
            {
                await _feedback.SendErrorAsync(this.Context, "You don't have any characters.");
                return;
            }

            if (userCharacters.Count == 1)
            {
                await _feedback.SendErrorAsync(this.Context, "You only have one character.");
                return;
            }

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                this.Context,
                invoker
            );

            // Filter out the current character, so becoming the same character isn't possible
            if (getCurrentCharacterResult.IsSuccess)
            {
                userCharacters = userCharacters.Except(new[] { getCurrentCharacterResult.Entity }).ToList();
            }

            var randomIndex = _random.Next(0, userCharacters.Count);

            var randomCharacter = userCharacters[randomIndex];
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
            var getInvokerResult = await _users.GetOrRegisterUserAsync(this.Context.User);
            if (!getInvokerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getInvokerResult.ErrorReason);
                return;
            }

            var invoker = getInvokerResult.Entity;

            var getPreviousCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                this.Context,
                invoker
            );

            CharacterRole? previousRole = null;
            if (getPreviousCharacterResult.IsSuccess)
            {
                previousRole = getPreviousCharacterResult.Entity.Role;
            }

            await _characters.MakeCharacterCurrentOnServerAsync(this.Context, this.Context.Guild, character);

            var guildUser = (IGuildUser)this.Context.User;
            var getServerResult = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
            if (!getServerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getServerResult.ErrorReason);
                return;
            }

            var server = getServerResult.Entity;

            if (!character.Nickname.IsNullOrWhitespace())
            {
                var modifyNickResult = await _discord.SetUserNicknameAsync(this.Context, guildUser, character.Nickname);
                if (!modifyNickResult.IsSuccess && !server.SuppressPermissionWarnings)
                {
                    await _feedback.SendWarningAsync(this.Context, modifyNickResult.ErrorReason);
                }
            }

            if (previousRole != character.Role)
            {
                if (!(previousRole is null))
                {
                    var previousDiscordRole = this.Context.Guild.GetRole((ulong)previousRole.DiscordID);
                    var removePreviousRoleResult = await _discord.RemoveUserRoleAsync
                    (
                        this.Context,
                        guildUser,
                        previousDiscordRole
                    );

                    if (!removePreviousRoleResult.IsSuccess)
                    {
                        await _feedback.SendErrorAsync(this.Context, removePreviousRoleResult.ErrorReason);
                    }
                }

                if (!(character.Role is null))
                {
                    var newDiscordRole = this.Context.Guild.GetRole((ulong)character.Role.DiscordID);
                    var addNewRoleResult = await _discord.AddUserRoleAsync
                    (
                        this.Context,
                        guildUser,
                        newDiscordRole
                    );

                    if (!addNewRoleResult.IsSuccess)
                    {
                        await _feedback.SendErrorAsync(this.Context, addNewRoleResult.ErrorReason);
                    }
                }
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
            var getUserResult = await _users.GetOrRegisterUserAsync(this.Context.User);
            if (!getUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;

            var result = await _characters.ClearDefaultCharacterForUserAsync(this.Context, user);
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
            var getInvokerResult = await _users.GetOrRegisterUserAsync(this.Context.User);
            if (!getInvokerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getInvokerResult.ErrorReason);
                return;
            }

            var invoker = getInvokerResult.Entity;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync
            (
                this.Context,
                invoker
            );

            var getDefaultCharacterResult = await _characters.GetDefaultCharacterAsync
            (
                invoker,
                this.Context.Guild
            );

            if (getCurrentCharacterResult.IsSuccess)
            {
                if (getDefaultCharacterResult.IsSuccess &&
                    getDefaultCharacterResult.Entity == getCurrentCharacterResult.Entity)
                {
                    await _feedback.SendErrorAsync(this.Context, "You're already your default form.");
                    return;
                }
            }

            var result = await _characters.ClearCurrentCharacterOnServerAsync(invoker, this.Context.Guild);
            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            if (this.Context.Message.Author is IGuildUser guildUser)
            {
                var getServerResult = await _servers.GetOrRegisterServerAsync(this.Context.Guild);
                if (!getServerResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getServerResult.ErrorReason);
                    return;
                }

                var server = getServerResult.Entity;

                ModifyEntityResult modifyNickResult;
                if (getDefaultCharacterResult.IsSuccess && !getDefaultCharacterResult.Entity.Nickname.IsNullOrWhitespace())
                {
                    modifyNickResult = await _discord.SetUserNicknameAsync
                    (
                        this.Context,
                        guildUser,
                        getDefaultCharacterResult.Entity.Nickname
                    );
                }
                else
                {
                    modifyNickResult = await _discord.SetUserNicknameAsync(this.Context, guildUser, guildUser.Username);
                }

                if (!modifyNickResult.IsSuccess && !server.SuppressPermissionWarnings)
                {
                    await _feedback.SendWarningAsync(this.Context, modifyNickResult.ErrorReason);
                }

                if (getCurrentCharacterResult.IsSuccess)
                {
                    var previousRole = getCurrentCharacterResult.Entity.Role;
                    if (!(previousRole is null))
                    {
                        var previousDiscordRole = this.Context.Guild.GetRole((ulong)previousRole.DiscordID);
                        var modifyRolesResult = await _discord.RemoveUserRoleAsync
                        (
                            this.Context, guildUser, previousDiscordRole
                        );

                        if (!modifyRolesResult.IsSuccess)
                        {
                            await _feedback.SendWarningAsync(this.Context, modifyRolesResult.ErrorReason);
                        }
                    }
                }
            }

            if (!getDefaultCharacterResult.IsSuccess)
            {
                await _feedback.SendConfirmationAsync(this.Context, "Character cleared.");
            }
            else
            {
                await AssumeCharacterFormAsync(getDefaultCharacterResult.Entity);
            }
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
            var addImageResult = await _characters.AddImageToCharacterAsync(character, imageName, imageUrl, imageCaption, isNSFW);
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
        /// <param name="imageName">The name of the image to remove.</param>
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
            var removeImageResult = await _characters.RemoveImageFromCharacterAsync(character, imageName);
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
            var getUserResult = await _users.GetOrRegisterUserAsync(newOwner);
            if (!getUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                return;
            }

            var user = getUserResult.Entity;

            var transferResult = await _characters.TransferCharacterOwnershipAsync(user, character, this.Context.Guild);
            if (!transferResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, transferResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Character ownership transferred.");
        }

        /// <summary>
        /// Role-related commands.
        /// </summary>
        [UsedImplicitly]
        [Group("role")]
        public class RoleCommands : ModuleBase
        {
            private readonly DiscordService _discord;
            private readonly UserFeedbackService _feedback;
            private readonly CharacterService _characters;

            /// <summary>
            /// Initializes a new instance of the <see cref="RoleCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="discordService">The Discord integration service.</param>
            /// <param name="feedbackService">The feedback service.</param>
            /// <param name="characterService">The character service.</param>
            /// <param name="servers">The server service.</param>
            public RoleCommands
            (
                CharactersDatabaseContext database,
                DiscordService discordService,
                UserFeedbackService feedbackService,
                CharacterService characterService,
                ServerService servers
            )
            {
                _discord = discordService;
                _feedback = feedbackService;
                _characters = characterService;
            }

            /// <summary>
            /// Lists the available character roles.
            /// </summary>
            [UsedImplicitly]
            [Command("list")]
            [RequireContext(ContextType.Guild)]
            public async Task ListAvailableRolesAsync()
            {
                var getServerRolesResult = await _characters.GetCharacterRolesAsync(this.Context.Guild);
                if (!getServerRolesResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getServerRolesResult.ErrorReason);
                    return;
                }

                var serverRoles = getServerRolesResult.Entity;

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

                if (!await serverRoles.AnyAsync())
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
                var createRoleResult = await _characters.CreateCharacterRoleAsync
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
                var getExistingRoleResult = await _characters.GetCharacterRoleAsync(discordRole);
                if (!getExistingRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getExistingRoleResult.ErrorReason);
                    return;
                }

                var currentCharactersWithRole = await _characters.GetCharacters(this.Context.Guild)
                    .Where(c => c.HasRole)
                    .Where(c => c.Role!.ID == getExistingRoleResult.Entity.ID)
                    .Where(c => c.IsCurrent)
                    .ToListAsync();

                var deleteRoleResult = await _characters.DeleteCharacterRoleAsync
                (getExistingRoleResult.Entity
                );

                if (!deleteRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, deleteRoleResult.ErrorReason);
                    return;
                }

                foreach (var character in currentCharactersWithRole)
                {
                    var owner = await this.Context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);
                    var role = this.Context.Guild.GetRole((ulong)getExistingRoleResult.Entity.DiscordID);

                    await _discord.RemoveUserRoleAsync(this.Context, owner, role);
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
                var getExistingRoleResult = await _characters.GetCharacterRoleAsync(discordRole);
                if (!getExistingRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getExistingRoleResult.ErrorReason);
                    return;
                }

                var setRoleAccessResult = await _characters.SetCharacterRoleAccessAsync
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
                var previousRole = character.Role;

                var result = await _characters.ClearCharacterRoleAsync(character);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                var guildUser = (IGuildUser)this.Context.User;
                if (!(previousRole is null))
                {
                    var previousDiscordRole = this.Context.Guild.GetRole((ulong)previousRole.DiscordID);
                    var modifyRolesResult = await _discord.RemoveUserRoleAsync
                    (
                        this.Context, guildUser, previousDiscordRole
                    );

                    if (!modifyRolesResult.IsSuccess)
                    {
                        await _feedback.SendWarningAsync(this.Context, modifyRolesResult.ErrorReason);
                    }
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character role cleared.");
            }
        }

        /// <summary>
        /// Property setter commands for characters.
        /// </summary>
        [UsedImplicitly]
        [Group("set")]
        public class SetCommands : ModuleBase
        {
            private readonly DiscordService _discord;
            private readonly UserFeedbackService _feedback;
            private readonly CharacterService _characters;
            private readonly UserService _users;

            /// <summary>
            /// Initializes a new instance of the <see cref="SetCommands"/> class.
            /// </summary>
            /// <param name="database">A database context from the context pool.</param>
            /// <param name="discordService">The Discord integration service.</param>
            /// <param name="feedbackService">The feedback service.</param>
            /// <param name="characterService">The character service.</param>
            /// <param name="servers">The server service.</param>
            /// <param name="users">The user service.</param>
            public SetCommands
            (
                CharactersDatabaseContext database,
                DiscordService discordService,
                UserFeedbackService feedbackService,
                CharacterService characterService,
                ServerService servers,
                UserService users
            )
            {
                _discord = discordService;
                _feedback = feedbackService;
                _characters = characterService;
                _users = users;
            }

            /// <summary>
            /// Sets the name of a character.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterName">The new name of the character.</param>
            [UsedImplicitly]
            [Command("name")]
            [Summary("Sets the name of a character.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterNameAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                string newCharacterName
            )
            {
                var setNameResult = await _characters.SetCharacterNameAsync(this.Context, character, newCharacterName);
                if (!setNameResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setNameResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character name set.");
            }

            /// <summary>
            /// Sets the avatar of a character. You can attach an image instead of passing a url as a parameter.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterAvatarUrl">The url of the new avatar. Optional.</param>
            [UsedImplicitly]
            [Command("avatar")]
            [Summary("Sets the avatar of a character. You can attach an image instead of passing a url as a parameter.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterAvatarAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                string? newCharacterAvatarUrl = null
            )
            {
                if (newCharacterAvatarUrl is null)
                {
                    if (!this.Context.Message.Attachments.Any())
                    {
                        await _feedback.SendErrorAsync(this.Context, "You need to attach an image or provide a url.");
                        return;
                    }

                    var newAvatar = this.Context.Message.Attachments.First();
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

                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character avatar set.");
            }

            /// <summary>
            /// Sets the nickname that the user should have when a character is active.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterNickname">The new nickname of the character. Max 32 characters.</param>
            [UsedImplicitly]
            [Alias("nickname", "nick")]
            [Command("nickname")]
            [Summary("Sets the nickname that the user should have when the character is active.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterNicknameAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                string newCharacterNickname
            )
            {
                var setNickResult = await _characters.SetCharacterNicknameAsync(character, newCharacterNickname);
                if (!setNickResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setNickResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character nickname set.");

                // Update the user's active nickname if they are currently this character, and don't have the same nick
                if (this.Context.User is IGuildUser guildUser && guildUser.Nickname != newCharacterNickname)
                {
                    if (!character.IsCurrent)
                    {
                        return;
                    }

                    var updateUserNickResult = await _discord.SetUserNicknameAsync
                    (
                        this.Context,
                        guildUser,
                        newCharacterNickname
                    );

                    if (!updateUserNickResult.IsSuccess)
                    {
                        await _feedback.SendWarningAsync
                        (
                            this.Context,
                            "Your character nickname has been updated, but I couldn't update your current real nickname due to permission issues (or something else). You'll have to do it yourself :("
                        );
                    }
                }
            }

            /// <summary>
            /// Sets the summary of a character.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterSummary">The new summary. Max 240 characters.</param>
            [UsedImplicitly]
            [Command("summary")]
            [Summary("Sets the summary of a character.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterSummaryAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                string newCharacterSummary
            )
            {
                var setSummaryResult = await _characters.SetCharacterSummaryAsync(character, newCharacterSummary);
                if (!setSummaryResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setSummaryResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character summary set.");
            }

            /// <summary>
            /// Sets the description of a character. You can attach a plaintext document instead of passing the contents as a parameter.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="newCharacterDescription">The new description of the character. Optional.</param>
            [UsedImplicitly]
            [Alias("description", "desc")]
            [Command("description")]
            [Summary("Sets the description of a character. You can attach a plaintext document instead of passing the contents as a parameter.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterDescriptionAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                string? newCharacterDescription = null
            )
            {
                if (newCharacterDescription is null)
                {
                    if (!this.Context.Message.Attachments.Any())
                    {
                        await _feedback.SendErrorAsync(this.Context, "You need to attach a plaintext document or provide an in-message description.");
                        return;
                    }

                    var newDescription = this.Context.Message.Attachments.First();
                    var getAttachmentStreamResult = await _discord.GetAttachmentStreamAsync(newDescription);
                    if (!getAttachmentStreamResult.IsSuccess)
                    {
                        await _feedback.SendErrorAsync(this.Context, getAttachmentStreamResult.ErrorReason);
                        return;
                    }

                    using var sr = new StreamReader(getAttachmentStreamResult.Entity);
                    newCharacterDescription = await sr.ReadToEndAsync();
                }

                var setDescriptionResult = await _characters.SetCharacterDescriptionAsync(character, newCharacterDescription);
                if (!setDescriptionResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setDescriptionResult.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character description set.");
            }

            /// <summary>
            /// Sets whether or not a character is NSFW.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="isNSFW">Whether or not the character is NSFW.</param>
            [UsedImplicitly]
            [Command("nsfw")]
            [Summary("Sets whether or not a character is NSFW.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterIsNSFWAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                bool isNSFW
            )
            {
                await _characters.SetCharacterIsNSFWAsync(character, isNSFW);

                await _feedback.SendConfirmationAsync(this.Context, $"Character set to {(isNSFW ? "NSFW" : "SFW")}.");
            }

            /// <summary>
            /// Sets the preferred pronoun for a character.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="pronounFamily">The pronoun family.</param>
            [UsedImplicitly]
            [Alias("pronoun", "pronouns")]
            [Command("pronoun")]
            [Summary("Sets the preferred pronoun of a character.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterPronounAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                string pronounFamily
            )
            {
                var result = await _characters.SetCharacterPronounAsync(character, pronounFamily);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Preferred pronoun set.");
            }

            /// <summary>
            /// Sets the given character's display role.
            /// </summary>
            /// <param name="character">The character.</param>
            /// <param name="discordRole">The role.</param>
            [UsedImplicitly]
            [Command("role")]
            [Summary("Sets the given character's display role.")]
            [RequireContext(ContextType.Guild)]
            [RequirePermission(typeof(EditCharacter), PermissionTarget.Self)]
            public async Task SetCharacterRoleAsync
            (
                [RequireEntityOwnerOrPermission(typeof(EditCharacter), PermissionTarget.Other)]
                Character character,
                IRole discordRole
            )
            {
                var previousRole = character.Role;

                var getRoleResult = await _characters.GetCharacterRoleAsync(discordRole);
                if (!getRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getRoleResult.ErrorReason);
                    return;
                }

                var commandInvoker = (IGuildUser)this.Context.User;
                var characterOwner = await this.Context.Guild.GetUserAsync((ulong)character.Owner.DiscordID);

                var role = getRoleResult.Entity;
                if (role.Access == RoleAccess.Restricted)
                {
                    if (!commandInvoker.GuildPermissions.ManageRoles)
                    {
                        await _feedback.SendErrorAsync
                        (
                            this.Context,
                            "That role is restricted, and you must be able to manage roles to use it."
                        );

                        return;
                    }
                }

                var setRoleResult = await _characters.SetCharacterRoleAsync(character, role);
                if (!setRoleResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, setRoleResult.ErrorReason);
                    return;
                }

                if (character.IsCurrent)
                {
                    if (!(previousRole is null))
                    {
                        var previousDiscordRole = this.Context.Guild.GetRole((ulong)previousRole.DiscordID);
                        var removePreviousRoleResult = await _discord.RemoveUserRoleAsync
                        (
                            this.Context,
                            characterOwner,
                            previousDiscordRole
                        );

                        if (!removePreviousRoleResult.IsSuccess)
                        {
                            await _feedback.SendErrorAsync(this.Context, removePreviousRoleResult.ErrorReason);
                        }
                    }

                    if (!(character.Role is null))
                    {
                        var newDiscordRole = this.Context.Guild.GetRole((ulong)character.Role.DiscordID);
                        var addNewRoleResult = await _discord.AddUserRoleAsync
                        (
                            this.Context,
                            characterOwner,
                            newDiscordRole
                        );

                        if (!addNewRoleResult.IsSuccess)
                        {
                            await _feedback.SendErrorAsync(this.Context, addNewRoleResult.ErrorReason);
                        }
                    }
                }

                await _feedback.SendConfirmationAsync(this.Context, "Character role set.");
            }

            /// <summary>
            /// Sets your default form to your current character.
            /// </summary>
            [UsedImplicitly]
            [Command("default")]
            [Summary("Sets your default form to your current character.")]
            [RequireContext(ContextType.Guild)]
            public async Task SetDefaultCharacterAsync()
            {
                var getInvokerResult = await _users.GetOrRegisterUserAsync(this.Context.User);
                if (!getInvokerResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getInvokerResult.ErrorReason);
                    return;
                }

                var invoker = getInvokerResult.Entity;

                var result = await _characters.GetCurrentCharacterAsync(this.Context, invoker);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await SetDefaultCharacterAsync(result.Entity);
            }

            /// <summary>
            /// Sets your default form to the given character.
            /// </summary>
            /// <param name="character">The character to set as the default character.</param>
            [UsedImplicitly]
            [Command("default")]
            [Summary("Sets your default form to the given character.")]
            [RequireContext(ContextType.Guild)]
            public async Task SetDefaultCharacterAsync
            (
                [RequireEntityOwner]
                Character character
            )
            {
                var getUserResult = await _users.GetOrRegisterUserAsync(this.Context.User);
                if (!getUserResult.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, getUserResult.ErrorReason);
                    return;
                }

                var user = getUserResult.Entity;

                var result = await _characters.SetDefaultCharacterForUserAsync(this.Context, character, user);
                if (!result.IsSuccess)
                {
                    await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                    return;
                }

                await _feedback.SendConfirmationAsync(this.Context, "Default character set.");
            }
        }
    }
}
