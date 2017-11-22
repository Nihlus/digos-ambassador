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

using System.Threading.Tasks;

using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services;

using Discord;
using Discord.Commands;

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
		/// Transforms the given bodypart into the given species on yourself.
		/// </summary>
		/// <param name="bodyPart">The part to transform.</param>
		/// <param name="species">The species to transform it into.</param>
		[UsedImplicitly]
		[Command(RunMode = Async)]
		[Summary("Transforms the given bodypart into the given species on yourself.")]
		[RequirePermission(Transform)]
		public async Task ShiftAsync(Bodypart bodyPart, [NotNull] string species)
		{

		}

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

		}

		/// <summary>
		/// Lists the available transformations for a given bodypart. If no bodypart is provided, all the species will be listed.
		/// </summary>
		/// <param name="bodyPart">The part to list available transformations for. Optional.</param>
		[UsedImplicitly]
		[Command("list-available", RunMode = Async)]
		[Summary("Lists the available transformations for a given bodypart. If no bodypart is provided, all the species will be listed.")]
		public async Task ListAvailableTransformationsAsync([CanBeNull] Bodypart? bodyPart = null)
		{

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

		}

		/// <summary>
		/// Submits a new transformation for review. Attach it to the command.
		/// </summary>
		/// <returns></returns>
		[UsedImplicitly]
		[Command("submit")]
		[Summary("Submits a new transformation for review. Attach it to the command.")]
		public async Task SubmitTransformationAsync()
		{

		}
	}
}
