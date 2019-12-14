//
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
using System.Linq;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Extensions;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Discord.Feedback;
using DIGOS.Ambassador.Discord.Interactivity;
using DIGOS.Ambassador.Discord.Pagination;
using DIGOS.Ambassador.Discord.TypeReaders;
using DIGOS.Ambassador.Plugins.Characters.Extensions;
using DIGOS.Ambassador.Plugins.Characters.Model;
using DIGOS.Ambassador.Plugins.Characters.Services;
using DIGOS.Ambassador.Plugins.Core.Services.Users;
using DIGOS.Ambassador.Plugins.Transformations.Extensions;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Results;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Plugins.Transformations.CommandModules
{
    /// <summary>
    /// Transformation-related commands, such as transforming certain body parts or saving transforms as characters.
    /// </summary>
    [Alias("transform", "shift", "tf")]
    [Group("transform")]
    [Summary("Transformation-related commands, such as transforming certain body parts or saving transforms as characters.")]
    public class TransformationCommands : ModuleBase
    {
        [NotNull]
        private readonly UserService _users;

        [NotNull]
        private readonly UserFeedbackService _feedback;

        [NotNull]
        private readonly ContentService _content;

        [NotNull]
        private readonly CharacterService _characters;

        [NotNull]
        private readonly TransformationService _transformation;

        [NotNull]
        private readonly InteractivityService _interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationCommands"/> class.
        /// </summary>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="characters">The character service.</param>
        /// <param name="transformation">The transformation service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        /// <param name="users">The user service.</param>
        /// <param name="content">The content service.</param>
        public TransformationCommands
        (
            [NotNull] UserFeedbackService feedback,
            [NotNull] CharacterService characters,
            [NotNull] TransformationService transformation,
            [NotNull] InteractivityService interactivity,
            [NotNull] UserService users,
            [NotNull] ContentService content
        )
        {
            _feedback = feedback;
            _characters = characters;
            _transformation = transformation;
            _interactivity = interactivity;
            _users = users;
            _content = content;
        }

        /// <summary>
        /// Transforms the given bodypart into the given species on yourself.
        /// </summary>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="species">The species to transform it into.</param>
        [UsedImplicitly]
        [Priority(int.MinValue)]
        [Command]
        [Summary("Transforms the given bodypart into the given species on yourself.")]
        public async Task ShiftAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull]
            string species
        )
        => await ShiftAsync(this.Context.User, chirality, bodyPart, species);

        /// <summary>
        /// Transforms the given bodypart into the given species on yourself.
        /// </summary>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="species">The species to transform it into.</param>
        [UsedImplicitly]
        [Priority(int.MinValue)]
        [Command]
        [Summary("Transforms the given bodypart into the given species on yourself.")]
        public async Task ShiftAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull]
            string species
        )
        => await ShiftAsync(this.Context.User, Chirality.Center, bodyPart, species);

        /// <summary>
        /// Transforms the given bodypart into the given species on the target user.
        /// </summary>
        /// <param name="target">The user to transform.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="species">The species to transform it into.</param>
        [UsedImplicitly]
        [Priority(int.MinValue)]
        [Command]
        [Summary("Transforms the given bodypart of the target user into the given species.")]
        [RequireContext(Guild)]
        public async Task ShiftAsync
        (
            [NotNull]
            IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull]
            string species
        )
        => await ShiftAsync(target, Chirality.Center, bodyPart, species);

        /// <summary>
        /// Transforms the given bodypart into the given species on the target user.
        /// </summary>
        /// <param name="target">The user to transform.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="species">The species to transform it into.</param>
        [UsedImplicitly]
        [Priority(int.MinValue)]
        [Command]
        [Summary("Transforms the given bodypart of the target user into the given species.")]
        [RequireContext(Guild)]
        public async Task ShiftAsync
        (
            [NotNull]
            IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull] string species
        )
        {
            var getTargetUserResult = await _users.GetOrRegisterUserAsync(target);
            if (!getTargetUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getTargetUserResult.ErrorReason);
                return;
            }

            var targetUser = getTargetUserResult.Entity;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync(this.Context, targetUser);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            ShiftBodypartResult result;
            if (species.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                result = await _transformation.RemoveBodypartAsync(this.Context, character, bodyPart, chirality);
            }
            else
            {
                result = await _transformation.ShiftBodypartAsync(this.Context, character, bodyPart, species, chirality);
            }

            if (!result.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, result.ShiftMessage);
        }

        /// <summary>
        /// Transforms the base colour of the given bodypart on yourself into the given colour.
        /// </summary>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("colour")]
        [Summary("Transforms the base colour of the given bodypart on yourself into the given colour.")]
        public async Task ShiftColourAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodypart,
            [NotNull]
            Colour colour
        )
        => await ShiftColourAsync(this.Context.User, chirality, bodypart, colour);

        /// <summary>
        /// Transforms the base colour of the given bodypart on yourself into the given colour.
        /// </summary>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("colour")]
        [Summary("Transforms the base colour of the given bodypart on yourself into the given colour.")]
        public async Task ShiftColourAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodypart,
            [NotNull]
            Colour colour
        )
        => await ShiftColourAsync(this.Context.User, Chirality.Center, bodypart, colour);

        /// <summary>
        /// Transforms the base colour of the given bodypart on the target user into the given colour.
        /// </summary>
        /// <param name="target">The target user.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Summary("Transforms the base colour of the given bodypart on the target user into the given colour.")]
        [Command("colour")]
        [RequireContext(Guild)]
        public async Task ShiftColourAsync
        (
            [NotNull] IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull] Colour colour
        )
        => await ShiftColourAsync(target, Chirality.Center, bodyPart, colour);

        /// <summary>
        /// Transforms the base colour of the given bodypart on the target user into the given colour.
        /// </summary>
        /// <param name="target">The target user.</param>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Summary("Transforms the base colour of the given bodypart on the target user into the given colour.")]
        [Command("colour")]
        [RequireContext(Guild)]
        public async Task ShiftColourAsync
        (
            [NotNull] IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull] Colour colour
        )
        {
            var getTargetUserResult = await _users.GetOrRegisterUserAsync(target);
            if (!getTargetUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getTargetUserResult.ErrorReason);
                return;
            }

            var targetUser = getTargetUserResult.Entity;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync(this.Context, targetUser);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await _transformation.ShiftBodypartColourAsync
            (
                this.Context,
                character,
                bodyPart,
                colour,
                chirality
            );

            if (!shiftPartResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, shiftPartResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, shiftPartResult.ShiftMessage);
        }

        /// <summary>
        /// Transforms the pattern on the given bodypart on yourself into the given pattern and secondary colour.
        /// </summary>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="pattern">The pattern to transform it into.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern")]
        [Summary("Transforms the pattern on the given bodypart on yourself into the given pattern and secondary colour.")]
        public async Task ShiftPatternAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodypart,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Pattern>))]
            Pattern pattern,
            [NotNull]
            Colour colour
        )
        => await ShiftPatternAsync(this.Context.User, Chirality.Center, bodypart, pattern, colour);

        /// <summary>
        /// Transforms the pattern on the given bodypart on yourself into the given pattern and secondary colour.
        /// </summary>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="pattern">The pattern to transform it into.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern")]
        [Summary("Transforms the pattern on the given bodypart on yourself into the given pattern and secondary colour.")]
        public async Task ShiftPatternAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodypart,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Pattern>))]
            Pattern pattern,
            [NotNull]
            Colour colour
        )
        => await ShiftPatternAsync(this.Context.User, chirality, bodypart, pattern, colour);

        /// <summary>
        /// Transforms the pattern on the given bodypart on the target user into the given pattern and secondary colour.
        /// </summary>
        /// <param name="target">The target user.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="pattern">The pattern to transform it into.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern")]
        [Summary("Transforms the pattern on the given bodypart on the target user into the given pattern and secondary colour.")]
        [RequireContext(Guild)]
        public async Task ShiftPatternAsync
        (
            [NotNull] IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Pattern>))]
            Pattern pattern,
            [NotNull] Colour colour
        )
        => await ShiftPatternAsync(target, Chirality.Center, bodyPart, pattern, colour);

        /// <summary>
        /// Transforms the pattern on the given bodypart on the target user into the given pattern and secondary colour.
        /// </summary>
        /// <param name="target">The target user.</param>
        /// <param name="chirality">The chirality of the part.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="pattern">The pattern to transform it into.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern")]
        [Summary("Transforms the pattern on the given bodypart on the target user into the given pattern and secondary colour.")]
        [RequireContext(Guild)]
        public async Task ShiftPatternAsync
        (
            [NotNull]
            IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Pattern>))]
            Pattern pattern,
            [NotNull]
            Colour colour
        )
        {
            var getTargetUserResult = await _users.GetOrRegisterUserAsync(target);
            if (!getTargetUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getTargetUserResult.ErrorReason);
                return;
            }

            var targetUser = getTargetUserResult.Entity;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync(this.Context, targetUser);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await _transformation.ShiftBodypartPatternAsync
            (
                this.Context,
                character,
                bodyPart,
                pattern,
                colour,
                chirality
            );

            if (!shiftPartResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, shiftPartResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, shiftPartResult.ShiftMessage);
        }

        /// <summary>
        /// Transforms the colour of the pattern on the given bodypart to the given colour.
        /// </summary>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern-colour")]
        [Summary("Transforms the colour of the pattern on the given bodypart on yourself to the given colour.")]
        public async Task ShiftPatternColourAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodypart,
            [NotNull]
            Colour colour
        )
        => await ShiftPatternColourAsync(this.Context.User, chirality, bodypart, colour);

        /// <summary>
        /// Transforms the colour of the pattern on the given bodypart to the given colour.
        /// </summary>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern-colour")]
        [Summary("Transforms the colour of the pattern on the given bodypart on yourself to the given colour.")]
        public async Task ShiftPatternColourAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodypart,
            [NotNull]
            Colour colour
        )
            => await ShiftPatternColourAsync(this.Context.User, Chirality.Center, bodypart, colour);

        /// <summary>
        /// Transforms the colour of the pattern on the given bodypart on the target user to the given colour.
        /// </summary>
        /// <param name="target">The target user.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern-colour")]
        [Summary("Transforms the colour of the pattern on the given bodypart on the target user to the given colour.")]
        [RequireContext(Guild)]
        public async Task ShiftPatternColourAsync
        (
            [NotNull] IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull] Colour colour
        )
        => await ShiftPatternColourAsync(target, Chirality.Center, bodyPart, colour);

        /// <summary>
        /// Transforms the colour of the pattern on the given bodypart on the target user to the given colour.
        /// </summary>
        /// <param name="target">The target user.</param>
        /// <param name="chirality">The chirality of the part.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern-colour")]
        [Summary("Transforms the colour of the pattern on the given bodypart on the target user to the given colour.")]
        [RequireContext(Guild)]
        public async Task ShiftPatternColourAsync
        (
            [NotNull]
            IUser target,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Chirality>))]
            Chirality chirality,
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart,
            [NotNull]
            Colour colour
        )
        {
            var getTargetUserResult = await _users.GetOrRegisterUserAsync(target);
            if (!getTargetUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getTargetUserResult.ErrorReason);
                return;
            }

            var targetUser = getTargetUserResult.Entity;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync(this.Context, targetUser);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await _transformation.ShiftPatternColourAsync(this.Context, character, bodyPart, colour, chirality);

            if (!shiftPartResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, shiftPartResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, shiftPartResult.ShiftMessage);
        }

        /// <summary>
        /// Lists the available transformation species.
        /// </summary>
        [UsedImplicitly]
        [Alias("list-available", "list-species", "species", "list")]
        [Command("list-available")]
        [Summary("Lists the available transformation species.")]
        public async Task ListAvailableTransformationsAsync()
        {
            var availableSpecies = await _transformation.GetAvailableSpeciesAsync();

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Available species";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                availableSpecies,
                s => $"{s.Name.Humanize(LetterCasing.Title)} ({s.Name})",
                s => $"{s.Description}\nWritten by {s.Author}.",
                "There are no species available.",
                appearance
            );

            if (availableSpecies.Any())
            {
                paginatedEmbed.WithPages
                (
                    paginatedEmbed.Pages.Select
                    (
                        p => p.WithDescription("Use the name inside the parens when transforming body parts.")
                    )
                );
            }

            await _interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );
        }

        /// <summary>
        /// Lists the available bodyparts.
        /// </summary>
        [UsedImplicitly]
        [Alias("list-bodyparts", "bodyparts", "parts")]
        [Command("parts")]
        [Summary("Lists the available bodyparts.")]
        public async Task ListAvailableBodypartsAsync()
        {
            var parts = Enum.GetValues(typeof(Bodypart))
                .Cast<Bodypart>()
                .OrderBy(b => b);

            var options = new PaginatedAppearanceOptions
            {
                Color = Color.DarkPurple
            };

            var paginatedMessage = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                parts,
                b => b.Humanize(),
                b =>
                {
                    if (b.IsChiral())
                    {
                        return "This part is available in both left and right versions.";
                    }

                    if (!b.IsGenderNeutral())
                    {
                        return "This part is considered NSFW.";
                    }

                    if (b.IsComposite())
                    {
                        return "This part is composed of smaller parts.";
                    }

                    return "This is a normal bodypart.";
                },
                appearance: options
            );

            await _interactivity.SendInteractiveMessageAsync(this.Context.Channel, paginatedMessage);
        }

        /// <summary>
        /// Lists the available shades.
        /// </summary>
        [UsedImplicitly]
        [Alias("list-colours", "list-shades", "colours", "shades")]
        [Command("colours")]
        [Summary("Lists the available colours.")]
        public async Task ListAvailableShadesAsync()
        {
            var parts = Enum.GetValues(typeof(Shade))
                .Cast<Shade>()
                .OrderBy(s => s);

            var options = new PaginatedAppearanceOptions
            {
                Color = Color.DarkPurple
            };

            var paginatedMessage = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                parts,
                b => b.Humanize(),
                b => "\u200B",
                appearance: options
            );

            await _interactivity.SendInteractiveMessageAsync(this.Context.Channel, paginatedMessage);
        }

        /// <summary>
        /// Lists the available shade modifiers.
        /// </summary>
        [UsedImplicitly]
        [Alias("list-colour-modifiers", "list-shade-modifiers", "colour-modifiers", "shade-modifiers")]
        [Command("colour-modifiers")]
        [Summary("Lists the available colour modifiers.")]
        public async Task ListAvailableShadeModifiersAsync()
        {
            var parts = Enum.GetValues(typeof(ShadeModifier))
                .Cast<ShadeModifier>()
                .OrderBy(sm => sm);

            var options = new PaginatedAppearanceOptions
            {
                Color = Color.DarkPurple
            };

            var paginatedMessage = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                parts,
                b => b.Humanize(),
                b => "\u200B",
                appearance: options
            );

            await _interactivity.SendInteractiveMessageAsync(this.Context.Channel, paginatedMessage);
        }

        /// <summary>
        /// Lists the available patterns.
        /// </summary>
        [UsedImplicitly]
        [Alias("list-patterns", "patterns")]
        [Command("colour-patterns")]
        [Summary("Lists the available patterns.")]
        public async Task ListAvailablePatternsAsync()
        {
            var parts = Enum.GetValues(typeof(Pattern))
                .Cast<Pattern>()
                .OrderBy(c => c);

            var options = new PaginatedAppearanceOptions
            {
                Color = Color.DarkPurple
            };

            var paginatedMessage = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                _feedback,
                this.Context.User,
                parts,
                b => b.Humanize(),
                b => "\u200B",
                appearance: options
            );

            await _interactivity.SendInteractiveMessageAsync(this.Context.Channel, paginatedMessage);
        }

        /// <summary>
        /// Lists the available transformations for a given bodypart.
        /// </summary>
        /// <param name="bodyPart">The part to list available transformations for. Optional.</param>
        [UsedImplicitly]
        [Alias("list-available", "list-species", "species", "list")]
        [Command("list-available")]
        [Summary("Lists the available transformations for a given bodypart.")]
        public async Task ListAvailableTransformationsAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart
        )
        {
            var transformations = await _transformation.GetAvailableTransformationsAsync(bodyPart);

            var eb = _feedback.CreateEmbedBase();
            eb.WithTitle("Available transformations");

            if (transformations.Count <= 0)
            {
                eb.WithDescription("There are no available transformations for this bodypart.");
            }
            else
            {
                eb.WithDescription("Use the name inside the parens when transforming body parts.");
            }

            foreach (var transformation in transformations)
            {
                var speciesName = $"{transformation.Species.Name.Humanize(LetterCasing.Title)} ({transformation.Species.Name})";
                eb.AddField(speciesName, transformation.Description);
            }

            await _feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build());
        }

        /// <summary>
        /// Describes the current physical appearance of the current character.
        /// </summary>
        [UsedImplicitly]
        [Command("describe")]
        [Summary("Describes the current physical appearance of the current character.")]
        public async Task DescribeCharacterAsync()
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

            await DescribeCharacterAsync(result.Entity);
        }

        /// <summary>
        /// Describes the current physical appearance of a character.
        /// </summary>
        /// <param name="character">The character to describe.</param>
        [UsedImplicitly]
        [Command("describe")]
        [Summary("Describes the current physical appearance of a character.")]
        public async Task DescribeCharacterAsync([NotNull] Character character)
        {
            var generateDescriptionAsync = await _transformation.GenerateCharacterDescriptionAsync
            (character
            );

            if (!generateDescriptionAsync.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, generateDescriptionAsync.ErrorReason);
                return;
            }

            var eb = new EmbedBuilder();
            eb.WithColor(Color.DarkPurple);
            eb.WithTitle($"{character.Name} \"{character.Nickname}\"".Trim());

            var owner = character.Owner;
            var user = await this.Context.Client.GetUserAsync((ulong)owner.DiscordID);
            eb.WithAuthor(user);

            eb.WithThumbnailUrl
            (
                !character.AvatarUrl.IsNullOrWhitespace()
                    ? character.AvatarUrl
                    : _content.GetDefaultAvatarUri().ToString()
            );

            eb.AddField("Description", character.Description);

            var description = generateDescriptionAsync.Entity;

            await _feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build());
            await _feedback.SendPrivateEmbedAsync
            (
                this.Context,
                this.Context.User,
                Color.DarkPurple,
                description,
                false
            );
        }

        /// <summary>
        /// Resets your form to your default one.
        /// </summary>
        [UsedImplicitly]
        [Alias("reset")]
        [Command("reset")]
        [Summary("Resets your form to your default one.")]
        public async Task ResetFormAsync()
        {
            var getInvokerResult = await _users.GetOrRegisterUserAsync(this.Context.User);
            if (!getInvokerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getInvokerResult.ErrorReason);
                return;
            }

            var invoker = getInvokerResult.Entity;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync(this.Context, invoker);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;
            var resetFormResult = await _transformation.ResetCharacterFormAsync(character);
            if (!resetFormResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, resetFormResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Character form reset.");
        }

        /// <summary>
        /// Sets your current appearance as your current character's default one.
        /// </summary>
        [UsedImplicitly]
        [Alias("set-default", "save-default")]
        [Command("set-default")]
        [Summary("Sets your current appearance as your current character's default one.")]
        public async Task SetCurrentAppearanceAsDefaultAsync()
        {
            var getInvokerResult = await _users.GetOrRegisterUserAsync(this.Context.User);
            if (!getInvokerResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getInvokerResult.ErrorReason);
                return;
            }

            var invoker = getInvokerResult.Entity;

            var getCurrentCharacterResult = await _characters.GetCurrentCharacterAsync(this.Context, invoker);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var setDefaultAppearanceResult = await _transformation.SetCurrentAppearanceAsDefaultForCharacterAsync(character);
            if (!setDefaultAppearanceResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, setDefaultAppearanceResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Current appearance saved as the default one of this character.");
        }

        /// <summary>
        /// Sets your default setting for opting in or out of transformations on servers you join.
        /// </summary>
        /// <param name="shouldOptIn">Whether or not to opt in by default.</param>
        [UsedImplicitly]
        [Command("default-opt-in")]
        [Summary("Sets your default setting for opting in or out of transformations on servers you join.")]
        [RequireContext(Guild)]
        public async Task SetDefaultOptInOrOutOfTransformationsAsync(bool shouldOptIn = true)
        {
            var setDefaultOptInResult = await _transformation.SetDefaultOptInAsync(this.Context.User, shouldOptIn);
            if (!setDefaultOptInResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, setDefaultOptInResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync
            (
                this.Context,
                $"You're now opted {(shouldOptIn ? "in" : "out")} by default on new servers."
            );
        }

        /// <summary>
        /// Opts into the transformation module on this server.
        /// </summary>
        [UsedImplicitly]
        [Command("opt-in")]
        [Summary("Opts into the transformation module on this server.")]
        [RequireContext(Guild)]
        public async Task OptInToTransformationsAsync()
        {
            var optInResult = await _transformation.OptInUserAsync(this.Context.User, this.Context.Guild);
            if (!optInResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, optInResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Opted into transformations. Have fun!");
        }

        /// <summary>
        /// Opts into the transformation module on this server.
        /// </summary>
        [UsedImplicitly]
        [Command("opt-out")]
        [Summary("Opts out of the transformation module on this server.")]
        [RequireContext(Guild)]
        public async Task OptOutOfTransformationsAsync()
        {
            var optOutResult = await _transformation.OptOutUserAsync(this.Context.User, this.Context.Guild);
            if (!optOutResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, optOutResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "Opted out of transformations.");
        }

        /// <summary>
        /// Sets your default protection type for transformations on servers you join. Available types are Whitelist and Blacklist.
        /// </summary>
        /// <param name="protectionType">The protection type to use.</param>
        [UsedImplicitly]
        [Command("default-protection")]
        [Summary("Sets your default protection type for transformations on servers you join. Available types are Whitelist and Blacklist.")]
        public async Task SetDefaultProtectionTypeAsync(ProtectionType protectionType)
        {
            var setProtectionTypeResult = await _transformation.SetDefaultProtectionTypeAsync(this.Context.User, protectionType);
            if (!setProtectionTypeResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, setProtectionTypeResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, $"Default protection type set to \"{protectionType.Humanize()}\"");
        }

        /// <summary>
        /// Sets your protection type for transformations. Available types are Whitelist and Blacklist.
        /// </summary>
        /// <param name="protectionType">The protection type to use.</param>
        [UsedImplicitly]
        [Command("protection")]
        [Summary("Sets your protection type for transformations. Available types are Whitelist and Blacklist.")]
        [RequireContext(Guild)]
        public async Task SetProtectionTypeAsync(ProtectionType protectionType)
        {
            var setProtectionTypeResult = await _transformation.SetServerProtectionTypeAsync(this.Context.User, this.Context.Guild, protectionType);
            if (!setProtectionTypeResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, setProtectionTypeResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, $"Protection type set to \"{protectionType.Humanize()}\"");
        }

        /// <summary>
        /// Whitelists a user, allowing them to transform you.
        /// </summary>
        /// <param name="user">The user to whitelist.</param>
        [UsedImplicitly]
        [Command("whitelist")]
        [Summary("Whitelists a user, allowing them to transform you.")]
        public async Task WhitelistUserAsync([NotNull] IUser user)
        {
            var whitelistUserResult = await _transformation.WhitelistUserAsync(this.Context.User, user);
            if (!whitelistUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, whitelistUserResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "User whitelisted.");
        }

        /// <summary>
        /// Blacklists a user, preventing them from transforming you.
        /// </summary>
        /// <param name="user">The user to blacklist.</param>
        [UsedImplicitly]
        [Command("blacklist")]
        [Summary("Blacklists a user, preventing them from transforming you.")]
        public async Task BlacklistUserAsync([NotNull] IUser user)
        {
            var blacklistUserResult = await _transformation.BlacklistUserAsync(this.Context.User, user);
            if (!blacklistUserResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, blacklistUserResult.ErrorReason);
                return;
            }

            await _feedback.SendConfirmationAsync(this.Context, "User whitelisted.");
        }

        /// <summary>
        /// Updates the transformation database with the bundled definitions.
        /// </summary>
        [UsedImplicitly]
        [Command("update-db")]
        [Summary("Updates the transformation database with the bundled definitions.")]
        [RequireOwner]
        public async Task UpdateTransformationDatabaseAsync()
        {
            var updateTransformationsResult = await _transformation.UpdateTransformationDatabaseAsync();
            if (!updateTransformationsResult.IsSuccess)
            {
                await _feedback.SendErrorAsync(this.Context, updateTransformationsResult.ErrorReason);
                return;
            }

            var confirmationText =
                $" Database updated. {updateTransformationsResult.SpeciesAdded} species added, " +
                $"{updateTransformationsResult.TransformationsAdded} transformations added, " +
                $"{updateTransformationsResult.SpeciesUpdated} species updated, " +
                $"and {updateTransformationsResult.TransformationsUpdated} transformations updated.";

            await _feedback.SendConfirmationAsync(this.Context, confirmationText);
        }
    }
}
