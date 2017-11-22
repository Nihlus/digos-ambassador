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
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;

using static DIGOS.Ambassador.Permissions.Permission;
using static DIGOS.Ambassador.Permissions.PermissionTarget;

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
		private readonly UserFeedbackService Feedback;

		private readonly ContentService Content;

		private readonly CharacterService Characters;

		private readonly TransformationService Transformation;

		/// <summary>
		/// Initializes a new instance of the <see cref="TransformationCommands"/> class.
		/// </summary>
		/// <param name="feedback">The feedback service.</param>
		/// <param name="content">The content service.</param>
		/// <param name="characters">The character service.</param>
		/// <param name="transformation">The transformation service.</param>
		public TransformationCommands(UserFeedbackService feedback, ContentService content, CharacterService characters, TransformationService transformation)
		{
			this.Feedback = feedback;
			this.Content = content;
			this.Characters = characters;
			this.Transformation = transformation;
		}

		/// <summary>
		/// Transforms the given bodypart into the given species on yourself.
		/// </summary>
		/// <param name="bodyPart">The part to transform.</param>
		/// <param name="species">The species to transform it into.</param>
		[UsedImplicitly]
		[Command(RunMode = Async)]
		[Summary("Transforms the given bodypart into the given species on yourself.")]
		[RequirePermission(Transform)]
		public async Task ShiftAsync(Bodypart bodyPart, [NotNull] string species) =>
			await ShiftAsync(this.Context.User, bodyPart, species);

		/// <summary>
		/// Transforms the given bodypart into the given species on the target user.
		/// </summary>
		/// <param name="target">The user to transform.</param>
		/// <param name="bodyPart">The part to transform.</param>
		/// <param name="species">The species to transform it into.</param>
		[UsedImplicitly]
		[Command(RunMode = Async)]
		[Summary("Transforms the given bodypart of the target user into the given species.")]
		[RequirePermission(Transform, Other)]
		public async Task ShiftAsync([NotNull] IUser target, Bodypart bodyPart, [NotNull] string species)
		{
			using (var db = new GlobalInfoContext())
			{
				var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(db, this.Context, target);
				if (!getCurrentCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
					return;
				}

				var character = getCurrentCharacterResult.Entity;
				var shiftPartResult = await this.Transformation.ShiftCharacterBodypartAsync(db, this.Context, character, bodyPart, species);
				if (!shiftPartResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, shiftPartResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, shiftPartResult.ShiftMessage);
			}
		}

		/// <summary>
		/// Lists the available transformation species.
		/// </summary>
		[UsedImplicitly]
		[Command("list-available", RunMode = Async)]
		[Summary("Lists the available transformation species.")]
		public async Task ListAvailableTransformationsAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				var availableSpecies = await this.Transformation.GetAvailableSpeciesAsync(db);

				var eb = new EmbedBuilder();
				eb.WithTitle("Available species");

				if (availableSpecies.Count <= 0)
				{
					eb.WithDescription("There are no available species.");
				}

				foreach (var species in availableSpecies)
				{
					eb.AddField(species.Name, species.Description);
				}

				await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb);
			}
		}

		/// <summary>
		/// Lists the available transformations for a given bodypart.
		/// </summary>
		/// <param name="bodyPart">The part to list available transformations for. Optional.</param>
		[UsedImplicitly]
		[Command("list-available", RunMode = Async)]
		[Summary("Lists the available transformations for a given bodypart.")]
		public async Task ListAvailableTransformationsAsync(Bodypart bodyPart)
		{
			using (var db = new GlobalInfoContext())
			{
				var transformations = await this.Transformation.GetAvailableTransformations(db, bodyPart);

				var eb = new EmbedBuilder();
				eb.WithTitle("Available transformations");

				if (transformations.Count <= 0)
				{
					eb.WithDescription("There are no available transformations for this bodypart.");
				}

				foreach (var transformation in transformations)
				{
					eb.AddField(transformation.Species, transformation.Description);
				}

				await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb);
			}
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
			using (var db = new GlobalInfoContext())
			{
				var eb = await this.Transformation.GenerateCharacterDescriptionAsync(character);

				await this.Feedback.SendPrivateEmbedAsync(this.Context, this.Context.User, eb);
			}
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
			using (var db = new GlobalInfoContext())
			{
				var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(db, this.Context, this.Context.User);
				if (!getCurrentCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
					return;
				}

				var character = getCurrentCharacterResult.Entity;
				var resetFormResult = await this.Transformation.ResetCharacterFormAsync(db, character);
				if (!resetFormResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, resetFormResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Character form reset.");
			}
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
			using (var db = new GlobalInfoContext())
			{
				var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(db, this.Context, this.Context.User);
				if (!getCurrentCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
					return;
				}

				var character = getCurrentCharacterResult.Entity;
				var currentAppearance = character.TransformedAppearance;

				var cloneCharacterResult = await this.Characters.CreateCharacterFromAppearanceAsync(db, this.Context, newCharacterName, currentAppearance);
				if (!cloneCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, cloneCharacterResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Current appearance saved as new character \"{newCharacterName}\"");
			}
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
			using (var db = new GlobalInfoContext())
			{
				var getCurrentCharacterResult = await this.Characters.GetCurrentCharacterAsync(db, this.Context, this.Context.User);
				if (!getCurrentCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getCurrentCharacterResult.ErrorReason);
					return;
				}

				var character = getCurrentCharacterResult.Entity;

				var setDefaultAppearanceResult = await this.Transformation.SetCurrentAppearanceAsDefaultForCharacterAsync(db, character);
				if (!setDefaultAppearanceResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, setDefaultAppearanceResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Current appearance saved as the default one of this character.");
			}
		}

		/// <summary>
		/// Sets your protection type for transformations. Available types are Whitelist and Blacklist.
		/// </summary>
		/// <param name="protectionType">The protection type to use.</param>
		[UsedImplicitly]
		[Command("protection")]
		[Summary("Sets your protection type for transformations. Available types are Whitelist and Blacklist.")]
		public async Task SetProtectionTypeAsync(ProtectionType protectionType)
		{
			using (var db = new GlobalInfoContext())
			{
				var setProtectionTypeResult = await this.Transformation.SetUserProtectionTypeAsync(db, this.Context.User, protectionType);
				if (!setProtectionTypeResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, setProtectionTypeResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Protection type set to \"{protectionType.Humanize()}\"");
			}
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
			using (var db = new GlobalInfoContext())
			{
				var whitelistUserResult = await this.Transformation.WhitelistUserAsync(db, this.Context.User, user);
				if (!whitelistUserResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, whitelistUserResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "User whitelisted.");
			}
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
			using (var db = new GlobalInfoContext())
			{
				var blacklistUserResult = await this.Transformation.BlacklistUserAsync(db, this.Context.User, user);
				if (!blacklistUserResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, blacklistUserResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "User whitelisted.");
			}
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
			throw new NotImplementedException();
		}

		/// <summary>
		/// Submits a new transformation for review. Attach it to the command.
		/// </summary>
		[UsedImplicitly]
		[Command("submit")]
		[Summary("Submits a new transformation for review. Attach it to the command.")]
		public async Task SubmitTransformationAsync()
		{
			throw new NotImplementedException();
		}
	}
}
