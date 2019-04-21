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

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Appearances;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Pagination;
using DIGOS.Ambassador.Services;
using DIGOS.Ambassador.Services.Interactivity;
using DIGOS.Ambassador.Transformations;
using DIGOS.Ambassador.TypeReaders;

using Discord;
using Discord.Commands;

using Humanizer;
using JetBrains.Annotations;
using static Discord.Commands.ContextType;
using static Discord.Commands.RunMode;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.Modules
{
    /// <summary>
    /// Transformation-related commands, such as transforming certain body parts or saving transforms as characters.
    /// </summary>
    [Alias("transform", "shift", "tf")]
    [Group("transform")]
    [Summary("Transformation-related commands, such as transforming certain body parts or saving transforms as characters.")]
    public class TransformationCommands : ModuleBase<SocketCommandContext>
    {
        [ProvidesContext]
        private readonly GlobalInfoContext Database;
        private readonly UserFeedbackService Feedback;

        private readonly CharacterService Characters;

        private readonly TransformationService Transformation;
        private readonly InteractivityService Interactivity;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationCommands"/> class.
        /// </summary>
        /// <param name="database">A database context from the context pool.</param>
        /// <param name="feedback">The feedback service.</param>
        /// <param name="characters">The character service.</param>
        /// <param name="transformation">The transformation service.</param>
        /// <param name="interactivity">The interactivity service.</param>
        public TransformationCommands
        (
            GlobalInfoContext database,
            UserFeedbackService feedback,
            CharacterService characters,
            TransformationService transformation,
            InteractivityService interactivity
        )
        {
            this.Database = database;
            this.Feedback = feedback;
            this.Characters = characters;
            this.Transformation = transformation;
            this.Interactivity = interactivity;
        }

        /// <summary>
        /// Transforms the given bodypart into the given species on yourself.
        /// </summary>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodyPart">The part to transform.</param>
        /// <param name="species">The species to transform it into.</param>
        [UsedImplicitly]
        [Priority(int.MinValue)]
        [Command(RunMode = Async)]
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
        [Command(RunMode = Async)]
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
        [Command(RunMode = Async)]
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
        [Command(RunMode = Async)]
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
            var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, target);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            ShiftBodypartResult result;
            if (species.Equals("remove", StringComparison.OrdinalIgnoreCase))
            {
                result = await this.Transformation.RemoveBodypartAsync(this.Database, this.Context, character, bodyPart, chirality);
            }
            else
            {
                result = await this.Transformation.ShiftBodypartAsync(this.Database, this.Context, character, bodyPart, species, chirality);
            }

            if (!result.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, result.ShiftMessage);
        }

        /// <summary>
        /// Transforms the base colour of the given bodypart on yourself into the given colour.
        /// </summary>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("colour", RunMode = Async)]
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
        [Command("colour", RunMode = Async)]
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
        [Command("colour", RunMode = Async)]
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
        [Command("colour", RunMode = Async)]
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
            var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, target);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await this.Transformation.ShiftBodypartColourAsync
            (
                this.Database,
                this.Context,
                character,
                bodyPart,
                colour,
                chirality
            );

            if (!shiftPartResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, shiftPartResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, shiftPartResult.ShiftMessage);
        }

        /// <summary>
        /// Transforms the pattern on the given bodypart on yourself into the given pattern and secondary colour.
        /// </summary>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="pattern">The pattern to transform it into.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern", RunMode = Async)]
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
        [Command("pattern", RunMode = Async)]
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
        [Command("pattern", RunMode = Async)]
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
        [Command("pattern", RunMode = Async)]
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
            var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, target);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await this.Transformation.ShiftBodypartPatternAsync
            (
                this.Database,
                this.Context,
                character,
                bodyPart,
                pattern,
                colour,
                chirality
            );

            if (!shiftPartResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, shiftPartResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, shiftPartResult.ShiftMessage);
        }

        /// <summary>
        /// Transforms the colour of the pattern on the given bodypart to the given colour.
        /// </summary>
        /// <param name="chirality">The chirality of the bodypart.</param>
        /// <param name="bodypart">The part to transform.</param>
        /// <param name="colour">The colour to transform it into.</param>
        [UsedImplicitly]
        [Command("pattern-colour", RunMode = Async)]
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
        [Command("pattern-colour", RunMode = Async)]
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
        [Command("pattern-colour", RunMode = Async)]
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
        [Command("pattern-colour", RunMode = Async)]
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
            var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, target);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var shiftPartResult = await this.Transformation.ShiftPatternColourAsync(this.Database, this.Context, character, bodyPart, colour, chirality);

            if (!shiftPartResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, shiftPartResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, shiftPartResult.ShiftMessage);
        }

        /// <summary>
        /// Lists the available transformation species.
        /// </summary>
        [UsedImplicitly]
        [Alias("list-available", "list-species", "species", "list")]
        [Command("list-available", RunMode = Async)]
        [Summary("Lists the available transformation species.")]
        public async Task ListAvailableTransformationsAsync()
        {
            var availableSpecies = await this.Transformation.GetAvailableSpeciesAsync(this.Database);

            var appearance = PaginatedAppearanceOptions.Default;
            appearance.Title = "Available species";

            var paginatedEmbed = PaginatedEmbedFactory.SimpleFieldsFromCollection
            (
                this.Feedback,
                this.Context.User,
                availableSpecies,
                s => $"{s.Name.Humanize(LetterCasing.Title)} ({s.Name})",
                s => s.Description ?? "No description set.",
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

            await this.Interactivity.SendInteractiveMessageAndDeleteAsync
            (
                this.Context.Channel,
                paginatedEmbed,
                TimeSpan.FromMinutes(5.0)
            );
        }

        /// <summary>
        /// Lists the available transformations for a given bodypart.
        /// </summary>
        /// <param name="bodyPart">The part to list available transformations for. Optional.</param>
        [UsedImplicitly]
        [Alias("list-available", "list-species", "species", "list")]
        [Command("list-available", RunMode = Async)]
        [Summary("Lists the available transformations for a given bodypart.")]
        public async Task ListAvailableTransformationsAsync
        (
            [OverrideTypeReader(typeof(HumanizerEnumTypeReader<Bodypart>))]
            Bodypart bodyPart
        )
        {
            var transformations = await this.Transformation.GetAvailableTransformationsAsync(this.Database, bodyPart);

            var eb = this.Feedback.CreateEmbedBase();
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

            await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb.Build());
        }

        /// <summary>
        /// Describes the current physical appearance of the current character.
        /// </summary>
        [UsedImplicitly]
        [Command("describe", RunMode = Async)]
        [Summary("Describes the current physical appearance of the current character.")]
        public async Task DescribeCharacterAsync()
        {
            var result = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Context.User);
            if (!result.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
                return;
            }

            await DescribeCharacterAsync(result.Entity);
        }

        /// <summary>
        /// Describes the current physical appearance of a character.
        /// </summary>
        /// <param name="character">The character to describe.</param>
        [UsedImplicitly]
        [Command("describe", RunMode = Async)]
        [Summary("Describes the current physical appearance of a character.")]
        public async Task DescribeCharacterAsync([NotNull] Character character)
        {
            var eb = await this.Transformation.GenerateCharacterDescriptionAsync(this.Context, character);

            await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb);
        }

        /// <summary>
        /// Resets your form to your default one.
        /// </summary>
        [UsedImplicitly]
        [Alias("reset")]
        [Command("reset", RunMode = Async)]
        [Summary("Resets your form to your default one.")]
        public async Task ResetFormAsync()
        {
            var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Context.User);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;
            var resetFormResult = await this.Transformation.ResetCharacterFormAsync(this.Database, character);
            if (!resetFormResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, resetFormResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, "Character form reset.");
        }

        /// <summary>
        /// Saves your current form as a new character.
        /// </summary>
        /// <param name="newCharacterName">The name of the character to save the form as.</param>
        [UsedImplicitly]
        [Alias("save", "save-current")]
        [Command("save", RunMode = Async)]
        [Summary("Saves your current form as a new character.")]
        public async Task SaveCurrentFormAsync([NotNull] string newCharacterName)
        {
            var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Context.User);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;
            var currentAppearance = character.CurrentAppearance;

            var cloneCharacterResult = await this.Characters.CreateCharacterFromAppearanceAsync(this.Database, this.Context, newCharacterName, currentAppearance);
            if (!cloneCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, cloneCharacterResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, $"Current appearance saved as new character \"{newCharacterName}\"");
        }

        /// <summary>
        /// Sets your current appearance as your current character's default one.
        /// </summary>
        [UsedImplicitly]
        [Alias("set-default", "save-default")]
        [Command("set-default", RunMode = Async)]
        [Summary("Sets your current appearance as your current character's default one.")]
        public async Task SetCurrentAppearanceAsDefaultAsync()
        {
            var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(this.Database, this.Context, this.Context.User);
            if (!getCurrentCharacterResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
                return;
            }

            var character = getCurrentCharacterResult.Entity;

            var setDefaultAppearanceResult = await this.Transformation.SetCurrentAppearanceAsDefaultForCharacterAsync(this.Database, character);
            if (!setDefaultAppearanceResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, setDefaultAppearanceResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, "Current appearance saved as the default one of this character.");
        }

        /// <summary>
        /// Sets your default setting for opting in or out of transformations on servers you join.
        /// </summary>
        /// <param name="shouldOptIn">Whether or not to opt in by default.</param>
        [UsedImplicitly]
        [Command("default-opt-in", RunMode = Async)]
        [Summary("Sets your default setting for opting in or out of transformations on servers you join.")]
        [RequireContext(Guild)]
        public async Task SetDefaultOptInOrOutOfTransformationsAsync(bool shouldOptIn = true)
        {
            var getProtectionResult = await this.Transformation.GetOrCreateGlobalUserProtectionAsync(this.Database, this.Context.User);
            if (!getProtectionResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getProtectionResult.ErrorReason);
                return;
            }

            var protection = getProtectionResult.Entity;

            protection.DefaultOptIn = shouldOptIn;

            await this.Database.SaveChangesAsync();

            await this.Feedback.SendConfirmationAsync
            (
                this.Context,
                $"You're now opted {(shouldOptIn ? "in" : "out")} by default on new servers."
            );
        }

        /// <summary>
        /// Opts into the transformation module on this server.
        /// </summary>
        [UsedImplicitly]
        [Command("opt-in", RunMode = Async)]
        [Summary("Opts into the transformation module on this server.")]
        [RequireContext(Guild)]
        public async Task OptInToTransformationsAsync()
        {
            var getProtectionResult = await this.Transformation.GetOrCreateServerUserProtectionAsync(this.Database, this.Context.User, this.Context.Guild);
            if (!getProtectionResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getProtectionResult.ErrorReason);
                return;
            }

            var protection = getProtectionResult.Entity;
            protection.HasOptedIn = true;

            await this.Database.SaveChangesAsync();

            await this.Feedback.SendConfirmationAsync(this.Context, "Opted into transformations. Have fun!");
        }

        /// <summary>
        /// Opts into the transformation module on this server.
        /// </summary>
        [UsedImplicitly]
        [Command("opt-out", RunMode = Async)]
        [Summary("Opts out of the transformation module on this server.")]
        [RequireContext(Guild)]
        public async Task OptOutOfTransformationsAsync()
        {
            var getProtectionResult = await this.Transformation.GetOrCreateServerUserProtectionAsync(this.Database, this.Context.User, this.Context.Guild);
            if (!getProtectionResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, getProtectionResult.ErrorReason);
                return;
            }

            var protection = getProtectionResult.Entity;
            protection.HasOptedIn = false;

            await this.Database.SaveChangesAsync();

            await this.Feedback.SendConfirmationAsync(this.Context, "Opted out of transformations.");
        }

        /// <summary>
        /// Sets your default protection type for transformations on servers you join. Available types are Whitelist and Blacklist.
        /// </summary>
        /// <param name="protectionType">The protection type to use.</param>
        [UsedImplicitly]
        [Command("default-protection", RunMode = Async)]
        [Summary("Sets your default protection type for transformations on servers you join. Available types are Whitelist and Blacklist.")]
        public async Task SetDefaultProtectionTypeAsync(ProtectionType protectionType)
        {
            var setProtectionTypeResult = await this.Transformation.SetDefaultProtectionTypeAsync(this.Database, this.Context.User, protectionType);
            if (!setProtectionTypeResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, setProtectionTypeResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, $"Default protection type set to \"{protectionType.Humanize()}\"");
        }

        /// <summary>
        /// Sets your protection type for transformations. Available types are Whitelist and Blacklist.
        /// </summary>
        /// <param name="protectionType">The protection type to use.</param>
        [UsedImplicitly]
        [Command("protection", RunMode = Async)]
        [Summary("Sets your protection type for transformations. Available types are Whitelist and Blacklist.")]
        [RequireContext(Guild)]
        public async Task SetProtectionTypeAsync(ProtectionType protectionType)
        {
            var setProtectionTypeResult = await this.Transformation.SetServerProtectionTypeAsync(this.Database, this.Context.User, this.Context.Guild, protectionType);
            if (!setProtectionTypeResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, setProtectionTypeResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, $"Protection type set to \"{protectionType.Humanize()}\"");
        }

        /// <summary>
        /// Whitelists a user, allowing them to transform you.
        /// </summary>
        /// <param name="user">The user to whitelist.</param>
        [UsedImplicitly]
        [Command("whitelist", RunMode = Async)]
        [Summary("Whitelists a user, allowing them to transform you.")]
        public async Task WhitelistUserAsync([NotNull] IUser user)
        {
            var whitelistUserResult = await this.Transformation.WhitelistUserAsync(this.Database, this.Context.User, user);
            if (!whitelistUserResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, whitelistUserResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, "User whitelisted.");
        }

        /// <summary>
        /// Blacklists a user, preventing them from transforming you.
        /// </summary>
        /// <param name="user">The user to blacklist.</param>
        [UsedImplicitly]
        [Command("blacklist", RunMode = Async)]
        [Summary("Blacklists a user, preventing them from transforming you.")]
        public async Task BlacklistUserAsync([NotNull] IUser user)
        {
            var blacklistUserResult = await this.Transformation.BlacklistUserAsync(this.Database, this.Context.User, user);
            if (!blacklistUserResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, blacklistUserResult.ErrorReason);
                return;
            }

            await this.Feedback.SendConfirmationAsync(this.Context, "User whitelisted.");
        }

        /// <summary>
        /// Updates the transformation database with the bundled definitions.
        /// </summary>
        [UsedImplicitly]
        [Command("update-db", RunMode = Async)]
        [Summary("Updates the transformation database with the bundled definitions.")]
        [RequireOwner]
        public async Task UpdateTransformationDatabaseAsync()
        {
            var updateTransformationsResult = await this.Transformation.UpdateTransformationDatabaseAsync(this.Database);
            if (!updateTransformationsResult.IsSuccess)
            {
                await this.Feedback.SendErrorAsync(this.Context, updateTransformationsResult.ErrorReason);
                return;
            }

            var confirmationText =
                $" Database updated. {updateTransformationsResult.SpeciesAdded} species added, " +
                $"{updateTransformationsResult.TransformationsAdded} transformations added, " +
                $"{updateTransformationsResult.SpeciesUpdated} species updated, " +
                $"and {updateTransformationsResult.TransformationsUpdated} transformations updated.";

            await this.Feedback.SendConfirmationAsync(this.Context, confirmationText);
        }
    }
}
