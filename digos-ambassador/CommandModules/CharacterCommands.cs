//
//  CharacterCommands.cs
//
//  Author:
//       Jarl Gullberg <jarl.gullberg@gmail.com>
//
//  Copyright (c) 2017 Jarl Gullberg
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Characters;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Permissions.Preconditions;
using DIGOS.Ambassador.Services.Characters;
using DIGOS.Ambassador.Services.Content;
using DIGOS.Ambassador.Services.Discord;
using DIGOS.Ambassador.Services.Feedback;

using Discord;
using Discord.Commands;

using JetBrains.Annotations;
using static DIGOS.Ambassador.Permissions.Permission;

#pragma warning disable SA1615 // Disable "Element return value should be documented" due to TPL tasks

namespace DIGOS.Ambassador.CommandModules
{
	/// <summary>
	/// Commands for interacting with characters.
	/// </summary>
	[UsedImplicitly]
	[Alias("character", "char", "ch")]
	[Group("character")]
	public class CharacterCommands : ModuleBase<SocketCommandContext>
	{
		private readonly ContentService Content;

		private readonly UserFeedbackService Feedback;

		private readonly CharacterService Characters;

		/// <summary>
		/// Initializes a new instance of the <see cref="CharacterCommands"/> class.
		/// </summary>
		/// <param name="contentService">The content service.</param>
		/// <param name="feedbackService">The feedback service.</param>
		/// <param name="characterService">The character service.</param>
		public CharacterCommands(ContentService contentService, UserFeedbackService feedbackService, CharacterService characterService)
		{
			this.Content = contentService;
			this.Feedback = feedbackService;
			this.Characters = characterService;
		}

		/// <summary>
		/// Shows quick information about a character.
		/// </summary>
		/// <param name="characterName">The name of the character.</param>
		/// <param name="characterOwner">The owner of the character.</param>
		[UsedImplicitly]
		[Command("show")]
		[Summary("Shows quick information about a character.")]
		public async Task ShowCharacterAsync
		(
			[NotNull] string characterName,
			[CanBeNull] IUser characterOwner = null
		)
		{
			using (var db = new GlobalInfoContext())
			{
				var getCharacterResult = await this.Characters.GetBestMatchingCharacter(db, this.Context, characterOwner, characterName);
				if (!getCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getCharacterResult.ErrorReason);
					return;
				}

				var character = getCharacterResult.Entity;
				var eb = CreateCharacterInfoEmbed(character);

				if (character.Description.Length + eb.Build().Length > 2000)
				{
					var userDMChannel = await this.Context.Message.Author.GetOrCreateDMChannelAsync();
					using (var ds = new MemoryStream(Encoding.UTF8.GetBytes(character.Description)))
					{
						await userDMChannel.SendMessageAsync(string.Empty, false, eb);
						await userDMChannel.SendFileAsync(ds, $"{character.Name}_description.txt");
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "Please check your private messages.");
				}
				else
				{
					eb.AddField("Description", character.Description);
					await this.Feedback.SendEmbedAsync(this.Context, eb);
				}
			}
		}

		[NotNull]
		private EmbedBuilder CreateCharacterInfoEmbed([NotNull] Character character)
		{
			var eb = new EmbedBuilder();

			eb.WithAuthor(this.Context.Client.GetUser(character.Owner.DiscordID));
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle(character.Name);
			eb.WithDescription(character.Summary);

			eb.WithImageUrl
			(
				!character.AvatarUrl.IsNullOrWhitespace()
					? character.AvatarUrl
					: this.Content.DefaultAvatarUri.ToString()
			);

			return eb;
		}

		/// <summary>
		/// Creates a new character.
		/// </summary>
		/// <param name="characterName">The name of the character.</param>
		/// <param name="characterAvatarUrl">A url pointing to the character's avatar. Optional.</param>
		/// <param name="characterNickname">The nickname that the user should assume when the character is active. Optional.</param>
		/// <param name="characterSummary">A summary of the character. Optional. Max 240 characters.</param>
		/// <param name="characterDescription">The full description of the character. Optional.</param>
		[UsedImplicitly]
		[Command("create")]
		[Summary("Creates a new character.")]
		[RequirePermission(CreateCharacter)]
		public async Task CreateCharacterAsync
		(
			[NotNull] string characterName,
			[CanBeNull] string characterAvatarUrl = null,
			[CanBeNull] string characterNickname = null,
			[NotNull] string characterSummary = "No summary set.",
			[NotNull] string characterDescription = "No description set."
		)
		{
			using (var db = new GlobalInfoContext())
			{
				characterAvatarUrl = characterAvatarUrl ?? this.Content.DefaultAvatarUri.ToString();

				var createCharacterResult = await this.Characters.CreateCharacterAsync
				(
					db,
					this.Context,
					characterName,
					characterAvatarUrl,
					characterNickname,
					characterSummary,
					characterDescription
				);
				if (!createCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, createCharacterResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"Character \"{createCharacterResult.Entity.Name}\" created.");
			}
		}

		/// <summary>
		/// Deletes the named character.
		/// </summary>
		/// <param name="characterName">The name of the character to delete.</param>
		[UsedImplicitly]
		[Command("delete")]
		[Summary("Deletes the named character.")]
		[RequirePermission(DeleteCharacter)]
		public async Task DeleteCharacterAsync([NotNull] string characterName)
		{
			using (var db = new GlobalInfoContext())
			{
				var getCharacterResult = await this.Characters.GetUserCharacterByNameAsync(db, this.Context, this.Context.Message.Author, characterName);
				if (!getCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getCharacterResult.ErrorReason);
					return;
				}

				var character = getCharacterResult.Entity;

				db.Characters.Remove(character);
				await db.SaveChangesAsync();

				await this.Feedback.SendConfirmationAsync(this.Context, $"Character \"{character.Name}\" deleted.");
			}
		}

		/// <summary>
		/// Lists the characters owned by a given user.
		/// </summary>
		/// <param name="discordUser">The user whose characters should be listed. Optional.</param>
		[UsedImplicitly]
		[Command("list-owned")]
		[Summary("Lists the characters owned by a given user.")]
		public async Task ListOwnedCharactersAsync([CanBeNull] IUser discordUser = null)
		{
			discordUser = discordUser ?? this.Context.Message.Author;

			var eb = new EmbedBuilder();
			eb.WithAuthor(discordUser);
			eb.WithColor(Color.DarkPurple);
			eb.WithTitle("Your charaters");

			using (var db = new GlobalInfoContext())
			{
				var characters = this.Characters.GetUserCharacters(db, discordUser);

				foreach (var character in characters)
				{
					eb.AddField(character.Name, character.Summary);
				}

				if (eb.Fields.Count <= 0)
				{
					eb.WithDescription("You don't have any characters.");
				}
			}

			await this.Feedback.SendEmbedAsync(this.Context, eb);
		}

		/// <summary>
		/// Sets the named character as the user's current character.
		/// </summary>
		/// <param name="characterName">The name of the character to become.</param>
		[UsedImplicitly]
		[Alias("assume", "become", "transform", "active")]
		[Command("assume")]
		[Summary("Sets the named characters as the user's current character.")]
		public async Task AssumeCharacterFormAsync([NotNull] string characterName)
		{
			using (var db = new GlobalInfoContext())
			{
				var getCharacterResult = await this.Characters.GetUserCharacterByNameAsync(db, this.Context, this.Context.Message.Author, characterName);
				if (!getCharacterResult.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, getCharacterResult.ErrorReason);
					return;
				}

				var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);
				var character = getCharacterResult.Entity;

				user.CurrentCharacter = character;

				if (!character.Nickname.IsNullOrWhitespace() && this.Context.Message.Author is IGuildUser guildUser)
				{
					await guildUser.ModifyAsync(u => u.Nickname = character.Nickname);
				}

				await this.Feedback.SendConfirmationAsync(this.Context, $"{this.Context.Message.Author.Username} shimmers and morphs into {character.Name}");
			}
		}

		/// <summary>
		/// Clears any active characters from you, restoring your default form.
		/// </summary>
		[UsedImplicitly]
		[Alias("clear", "drop", "default")]
		[Command("clear")]
		[Summary("Clears any active characters from you, restoring your default form.")]
		public async Task ClearCharacterFormAsync()
		{
			using (var db = new GlobalInfoContext())
			{
				var user = await db.GetOrRegisterUserAsync(this.Context.Message.Author);

				user.CurrentCharacter = user.DefaultCharacter;

				if (this.Context.Message.Author is IGuildUser guildUser)
				{
					if (!(user.DefaultCharacter?.Nickname).IsNullOrWhitespace())
					{
						await guildUser.ModifyAsync(u => u.Nickname = user.DefaultCharacter.Nickname);
					}
					else
					{
						await guildUser.ModifyAsync(u => u.Nickname = null);
					}
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Character cleared.");
			}
		}

		/// <summary>
		/// Transfers ownership of the named character to another user.
		/// </summary>
		/// <param name="newOwner">The new owner of the character.</param>
		/// <param name="characterName">The name of the character to transfer.</param>
		[UsedImplicitly]
		[Alias("transfer-ownership", "transfer")]
		[Command("transfer-ownership")]
		[Summary("Transfers ownership of the named character to another user.")]
		public async Task TransferCharacterOwnershipAsync([NotNull] IUser newOwner, [NotNull] string characterName)
		{
			using (var db = new GlobalInfoContext())
			{
				var result = await this.Characters.GetBestMatchingCharacter(db, this.Context, this.Context.Message.Author, characterName);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
					return;
				}

				var character = result.Entity;
				var transferResult = await this.Characters.TransferCharacterOwnershipAsync(db, newOwner, character);
				if (!result.IsSuccess)
				{
					await this.Feedback.SendErrorAsync(this.Context, transferResult.ErrorReason);
					return;
				}

				await this.Feedback.SendConfirmationAsync(this.Context, "Character ownership transferred.");
			}
		}

		/// <summary>
		/// Property setter commands for characters.
		/// </summary>
		[UsedImplicitly]
		[Group("set")]
		public class SetCommands : ModuleBase<SocketCommandContext>
		{
			private readonly DiscordService Discord;

			private readonly ContentService Content;

			private readonly UserFeedbackService Feedback;

			private readonly CharacterService Characters;

			/// <summary>
			/// Initializes a new instance of the <see cref="SetCommands"/> class.
			/// </summary>
			/// <param name="discordService">The Discord integration service.</param>
			/// <param name="contentService">The content service.</param>
			/// <param name="feedbackService">The feedback service.</param>
			/// <param name="characterService">The character service.</param>
			public SetCommands(DiscordService discordService, ContentService contentService, UserFeedbackService feedbackService, CharacterService characterService)
			{
				this.Discord = discordService;
				this.Content = contentService;
				this.Feedback = feedbackService;
				this.Characters = characterService;
			}

			/// <summary>
			/// Sets the name of a character.
			/// </summary>
			/// <param name="oldCharacterName">The name of the character.</param>
			/// <param name="newCharacterName">The new name of the character.</param>
			[UsedImplicitly]
			[Command("name")]
			[Summary("Sets the name of a character.")]
			[RequirePermission(EditCharacter)]
			public async Task SetCharacterNameAsync([NotNull] string oldCharacterName, [NotNull] string newCharacterName)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Characters.GetUserCharacterByNameAsync(db, this.Context, this.Context.Message.Author, oldCharacterName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var character = result.Entity;

					var setNameResult = await this.Characters.SetCharacterNameAsync(db, this.Context, character, newCharacterName);
					if (!setNameResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, setNameResult.ErrorReason);
						return;
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "Roleplay name set.");
				}
			}

			/// <summary>
			/// Sets the avatar of a character.
			/// </summary>
			/// <param name="characterName">The name of the character.</param>
			/// <param name="newCharacterAvatarUrl">The url of the new avatar. Optional.</param>
			[UsedImplicitly]
			[Command("avatar")]
			[Summary("Sets the avatar of a character. You can attach an image instead of passing a url as a parameter.")]
			[RequirePermission(EditCharacter)]
			public async Task SetCharacterAvatarAsync([NotNull] string characterName, [CanBeNull] string newCharacterAvatarUrl = null)
			{
				if (newCharacterAvatarUrl is null)
				{
					if (!this.Context.Message.Attachments.Any())
					{
						await this.Feedback.SendErrorAsync(this.Context, "You need to attach an image or provide a url.");
						return;
					}

					var newAvatar = this.Context.Message.Attachments.First();
					newCharacterAvatarUrl = newAvatar.Url;
				}

				using (var db = new GlobalInfoContext())
				{
					var result = await this.Characters.GetUserCharacterByNameAsync(db, this.Context, this.Context.Message.Author, characterName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var character = result.Entity;
					character.AvatarUrl = newCharacterAvatarUrl;

					await db.SaveChangesAsync();

					await this.Feedback.SendConfirmationAsync(this.Context, "Character avatar set.");
				}
			}

			/// <summary>
			/// Sets the nickname that the user should have when a character is active.
			/// </summary>
			/// <param name="characterName">The name of the character.</param>
			/// <param name="newCharacterNickname">The new nickname of the character. Max 32 characters.</param>
			[UsedImplicitly]
			[Alias("nickname", "nick")]
			[Command("nickname")]
			[Summary("Sets the nickname that the user should have when the character is active.")]
			[RequirePermission(EditCharacter)]
			public async Task SetCharacterNicknameAsync([NotNull] string characterName, [NotNull] string newCharacterNickname)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Characters.GetUserCharacterByNameAsync(db, this.Context, this.Context.Message.Author, characterName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var character = result.Entity;

					var setNickResult = await this.Characters.SetCharacterNicknameAsync(db, character, newCharacterNickname);
					if (!setNickResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, setNickResult.ErrorReason);
						return;
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "Character nickname set.");
				}
			}

			/// <summary>
			/// Sets the summary of a character.
			/// </summary>
			/// <param name="characterName">The name of the character.</param>
			/// <param name="newCharacterSummary">The new summary. Max 240 characters.</param>
			[UsedImplicitly]
			[Command("summary")]
			[Summary("Sets the summary of a character.")]
			[RequirePermission(EditCharacter)]
			public async Task SetCharacterSummaryAsync([NotNull] string characterName, [NotNull] string newCharacterSummary)
			{
				using (var db = new GlobalInfoContext())
				{
					var result = await this.Characters.GetUserCharacterByNameAsync(db, this.Context, this.Context.Message.Author, characterName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var character = result.Entity;

					var setSummaryResult = await this.Characters.SetCharacterSummaryAsync(db, character, newCharacterSummary);
					if (!setSummaryResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, setSummaryResult.ErrorReason);
						return;
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "Character summary set.");
				}
			}

			/// <summary>
			/// Sets the description of a character.
			/// </summary>
			/// <param name="characterName">The name of the character.</param>
			/// <param name="newCharacterDescription">The new description of the character. Optional.</param>
			[UsedImplicitly]
			[Alias("description", "desc")]
			[Command("description")]
			[Summary("Sets the description of a character. You can attach a plaintext document instead of passing it as a parameter.")]
			[RequirePermission(EditCharacter)]
			public async Task SetCharacterDescriptionAsync([NotNull] string characterName, [CanBeNull] string newCharacterDescription = null)
			{
				if (newCharacterDescription is null)
				{
					if (!this.Context.Message.Attachments.Any())
					{
						await this.Feedback.SendErrorAsync(this.Context, "You need to attach a plaintext document or provide an in-message description.");
						return;
					}

					var newDescription = this.Context.Message.Attachments.First();
					var getAttachmentStreamResult = await this.Discord.GetAttachmentStreamAsync(newDescription);
					if (!getAttachmentStreamResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, getAttachmentStreamResult.ErrorReason);
						return;
					}

					using (var s = getAttachmentStreamResult.Entity)
					{
						using (var sr = new StreamReader(s))
						{
							newCharacterDescription = sr.ReadToEnd();
						}
					}
				}

				using (var db = new GlobalInfoContext())
				{
					var result = await this.Characters.GetUserCharacterByNameAsync(db, this.Context, this.Context.Message.Author, characterName);
					if (!result.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, result.ErrorReason);
						return;
					}

					var character = result.Entity;

					var setDescriptionResult = await this.Characters.SetCharacterDescriptionAsync(db, character, newCharacterDescription);
					if (!setDescriptionResult.IsSuccess)
					{
						await this.Feedback.SendErrorAsync(this.Context, setDescriptionResult.ErrorReason);
						return;
					}

					await this.Feedback.SendConfirmationAsync(this.Context, "Character description set.");
				}
			}
		}
	}
}
