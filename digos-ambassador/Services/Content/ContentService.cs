//
//  ContentService.cs
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
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

using DIGOS.Ambassador.Database;
using DIGOS.Ambassador.Database.Dossiers;
using DIGOS.Ambassador.Database.Transformations;
using DIGOS.Ambassador.Extensions;
using DIGOS.Ambassador.Transformations;

using Discord.Commands;

using JetBrains.Annotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace DIGOS.Ambassador.Services
{
	/// <summary>
	/// Management class for content that comes bundled with the bot. Responsible for loading and providing access to
	/// the content.
	/// </summary>
	public class ContentService
	{
		/// <summary>
		/// Gets the Discord bot OAuth token.
		/// </summary>o
		public string BotToken { get; private set; }

		private List<string> Sass;
		private List<string> SassNSFW;

		private Uri BaseRemoteContentUri { get; }

		/// <summary>
		/// Gets the base content path.
		/// </summary>
		public string BaseContentPath { get; } = Path.GetFullPath(Path.Combine("Content"));

		/// <summary>
		/// Gets the base dossier path.
		/// </summary>
		public string BaseDossierPath { get; } = Path.GetFullPath(Path.Combine("Content", "Dossiers"));

		/// <summary>
		/// Gets the base dossier path.
		/// </summary>
		public string BaseTransformationSpeciesPath { get; } = Path.GetFullPath(Path.Combine("Content", "Transformations", "Species"));

		/// <summary>
		/// Gets the <see cref="Uri"/> pointing to the default avatar used by the bot for characters.
		/// </summary>
		public Uri DefaultAvatarUri { get; }

		/// <summary>
		/// Gets the <see cref="Uri"/> pointing to a portrait of Amby.
		/// </summary>
		public Uri AmbyPortraitUri { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ContentService"/> class.
		/// </summary>
		public ContentService()
		{
			this.BaseRemoteContentUri = new Uri("https://raw.githubusercontent.com/Nihlus/digos-ambassador/master/digos-ambassador/Content/");
			this.DefaultAvatarUri = new Uri(this.BaseRemoteContentUri, "Avatars/Default/Discord_DIGOS.png");

			this.AmbyPortraitUri = new Uri(this.BaseRemoteContentUri, "Portraits/amby-irbynx-3.png");
		}

		/// <summary>
		/// Loads the default content.
		/// </summary>
		/// <returns>A task wrapping the content load operation.</returns>
		public async Task InitializeAsync()
		{
			await LoadSassAsync();
			await LoadBotTokenAsync();

			if (!Directory.Exists(this.BaseDossierPath))
			{
				Directory.CreateDirectory(this.BaseDossierPath);
			}
		}

		/// <summary>
		/// Loads the sass from disk.
		/// </summary>
		private async Task LoadSassAsync()
		{
			var sassPath = Path.Combine(this.BaseContentPath, "Sass", "sass.txt");
			var sassNSFWPath = Path.Combine(this.BaseContentPath, "Sass", "sass-nsfw.txt");

			if (!File.Exists(sassPath))
			{
				this.Sass = new List<string>();
			}

			if (!File.Exists(sassNSFWPath))
			{
				this.SassNSFW = new List<string>();
			}

			this.Sass = (await FileAsync.ReadAllLinesAsync(sassPath)).ToList();
			this.SassNSFW = (await FileAsync.ReadAllLinesAsync(sassNSFWPath)).ToList();
		}

		/// <summary>
		/// Loads the bot token from disk.
		/// </summary>
		/// <exception cref="FileNotFoundException">Thrown if the bot token file can't be found.</exception>
		/// <exception cref="InvalidDataException">Thrown if no token exists in the file.</exception>
		private async Task LoadBotTokenAsync()
		{
			var tokenPath = Path.Combine(this.BaseContentPath, "bot.token");

			if (!File.Exists(tokenPath))
			{
				throw new FileNotFoundException("The bot token file could not be found.", tokenPath);
			}

			var token = await FileAsync.ReadAllTextAsync(tokenPath);

			if (string.IsNullOrEmpty(token))
			{
				throw new InvalidDataException("Missing bot token.");
			}

			this.BotToken = token;
		}

		/// <summary>
		/// Gets or creates a stream with the given content-local path.
		/// </summary>
		/// <param name="relativePath">The relative path inside the content directory.</param>
		/// <returns>A <see cref="FileStream"/> pointing to the path..</returns>
		public RetrieveEntityResult<FileStream> GetOrCreateLocalStream([PathReference] [NotNull] string relativePath)
		{
			var guaranteedRelativePath = relativePath.TrimStart('.', '\\', '/');
			var contentPath = Path.Combine(this.BaseContentPath, guaranteedRelativePath);

			var openStreamResult = OpenLocalStream(contentPath, fileMode: FileMode.OpenOrCreate);
			return openStreamResult;
		}

		/// <summary>
		/// Gets the stream of a local content file.
		/// </summary>
		/// <param name="path">The path to the file.</param>
		/// <param name="subdirectory">The subdirectory in the content folder, if any.</param>
		/// <param name="fileMode">The mode with which to open the stream.</param>
		/// <returns>A <see cref="FileStream"/> with the file data.</returns>
		[Pure]
		[MustUseReturnValue("The resulting file stream must be disposed.")]
		public RetrieveEntityResult<FileStream> OpenLocalStream([PathReference] [NotNull] string path, [CanBeNull] string subdirectory = null, FileMode fileMode = FileMode.Open)
		{
			var absolutePath = Path.GetFullPath(path);

			if (!absolutePath.StartsWith(this.BaseContentPath))
			{
				return RetrieveEntityResult<FileStream>.FromError
				(
					CommandError.Unsuccessful,
					"The path pointed to something that wasn't in the content folder."
				);
			}

			if (!(subdirectory is null))
			{
				var subdirectoryParentDir = Path.Combine(this.BaseContentPath, subdirectory);
				if (!absolutePath.StartsWith(subdirectoryParentDir))
				{
					return RetrieveEntityResult<FileStream>.FromError
					(
						CommandError.Unsuccessful,
						"The path pointed to something that wasn't in the specified subdirectory."
					);
				}
			}

			// Make sure that the directory chain is created
			Directory.CreateDirectory(Directory.GetParent(absolutePath).FullName);

			return RetrieveEntityResult<FileStream>.FromSuccess(File.Open(absolutePath, fileMode));
		}

		/// <summary>
		/// Gets a given dossier's data.
		/// </summary>
		/// <param name="dossier">The dossier to get the data for.</param>
		/// <returns>A <see cref="FileStream"/> containing the dossier data.</returns>
		[Pure]
		public RetrieveEntityResult<FileStream> GetDossierStream([NotNull] Dossier dossier)
		{
			if (!File.Exists(dossier.Path) || dossier.Path.IsNullOrWhitespace())
			{
				return RetrieveEntityResult<FileStream>.FromError(CommandError.ObjectNotFound, "No file data set.");
			}

			return OpenLocalStream(dossier.Path, "Dossiers");
		}

		/// <summary>
		/// Deletes the content data associated with a given dossier.
		/// </summary>
		/// <param name="dossier">The dossier.</param>
		/// <returns>A deletion result which may or may not have succeeded.</returns>
		public Task<DeleteEntityResult> DeleteDossierDataAsync([NotNull] Dossier dossier)
		{
			var dataPath = GetDossierDataPath(dossier);
			if (!File.Exists(dataPath))
			{
				return Task.FromResult(DeleteEntityResult.FromSuccess());
			}

			try
			{
				File.Delete(dataPath);
			}
			catch (Exception e)
			{
				return Task.FromResult(DeleteEntityResult.FromError(CommandError.Exception, e.Message));
			}

			return Task.FromResult(DeleteEntityResult.FromSuccess());
		}

		/// <summary>
		/// Gets the absolute path to where the data of the dossier is stored.
		/// </summary>
		/// <param name="dossier">The dossier.</param>
		/// <returns>The path.</returns>
		[Pure]
		[NotNull]
		public string GetDossierDataPath([NotNull] Dossier dossier)
		{
			return Path.GetFullPath(Path.Combine(this.BaseContentPath, "Dossiers", $"{dossier.Title}.pdf"));
		}

		/// <summary>
		/// Discovers the species that have been bundled with the program.
		/// </summary>
		/// <returns>A retrieval result which may or may not have suceeded.</returns>
		[Pure]
		public async Task<RetrieveEntityResult<IReadOnlyList<Species>>> DiscoverBundledSpeciesAsync()
		{
			const string speciesFilename = "Species.yml";

			var deser = new DeserializerBuilder()
				.WithNodeDeserializer(i => new ValidatingNodeDeserializer(i), s => s.InsteadOf<ObjectNodeDeserializer>())
				.WithNamingConvention(new UnderscoredNamingConvention())
				.Build();

			var species = new List<Species>();
			var speciesFolders = Directory.EnumerateDirectories(this.BaseTransformationSpeciesPath);
			foreach (string directory in speciesFolders)
			{
				string speciesFilePath = Path.Combine(directory, speciesFilename);
				if (!File.Exists(speciesFilePath))
				{
					continue;
				}

				string content = await FileAsync.ReadAllTextAsync(speciesFilePath, Encoding.UTF8);

				try
				{
					species.Add(deser.Deserialize<Species>(content));
				}
				catch (YamlException yex)
				{
					if (yex.InnerException is SerializationException sex)
					{
						return RetrieveEntityResult<IReadOnlyList<Species>>.FromError(sex);
					}

					return RetrieveEntityResult<IReadOnlyList<Species>>.FromError(yex);
				}
			}

			return RetrieveEntityResult<IReadOnlyList<Species>>.FromSuccess(species);
		}

		/// <summary>
		/// Discovers the transformations of a specific species that have been bundled with the program. The species
		/// must already be registered in the database.
		/// </summary>
		/// <param name="db">The database.</param>
		/// <param name="transformation">The transformation service.</param>
		/// <param name="species">The species to discover transformations for.</param>
		/// <returns>A retrieval result which may or may not have suceeded.</returns>
		[Pure]
		public async Task<RetrieveEntityResult<IReadOnlyList<Transformation>>> DiscoverBundledTransformationsAsync
		(
			[NotNull] GlobalInfoContext db,
			[NotNull] TransformationService transformation,
			[NotNull] Species species
		)
		{
			const string speciesFilename = "Species.yml";

			var speciesDir = GetSpeciesDirectory(species);
			var transformationFiles = Directory.EnumerateFiles(speciesDir).Where(p => !p.EndsWith(speciesFilename));

			var transformations = new List<Transformation>();
			var deser = new DeserializerBuilder()
				.WithTypeConverter(new ColourYamlConverter())
				.WithTypeConverter(new SpeciesYamlConverter(db, transformation))
				.WithNodeDeserializer(i => new ValidatingNodeDeserializer(i), s => s.InsteadOf<ObjectNodeDeserializer>())
				.WithNamingConvention(new UnderscoredNamingConvention())
				.Build();

			foreach (var transformationFile in transformationFiles)
			{
				string content = await FileAsync.ReadAllTextAsync(transformationFile);

				try
				{
					transformations.Add(deser.Deserialize<Transformation>(content));
				}
				catch (YamlException yex)
				{
					if (yex.InnerException is SerializationException sex)
					{
						return RetrieveEntityResult<IReadOnlyList<Transformation>>.FromError(sex);
					}

					return RetrieveEntityResult<IReadOnlyList<Transformation>>.FromError(yex);
				}
			}

			return RetrieveEntityResult<IReadOnlyList<Transformation>>.FromSuccess(transformations);
		}

		[Pure]
		private string GetSpeciesDirectory([NotNull] Species species)
		{
			return Path.Combine(this.BaseTransformationSpeciesPath, species.Name);
		}

		/// <summary>
		/// Gets a sassy comment.
		/// </summary>
		/// <param name="includeNSFW">Whether or not to include NSFW sass.</param>
		/// <returns>A sassy comment.</returns>
		[Pure]
		public string GetSass(bool includeNSFW = false)
		{
			if (includeNSFW)
			{
				return this.Sass.Union(this.SassNSFW).ToList().PickRandom();
			}

			return this.Sass.PickRandom();
		}

		/// <summary>
		/// Gets the absolute path to a named lua script belonging to the given transformation.
		/// </summary>
		/// <param name="transformation">The transformation that the script belongs to.</param>
		/// <param name="scriptName">The name of the script.</param>
		/// <returns>The path to the script.</returns>
		[Pure]
		[NotNull]
		public string GetLuaScriptPath([NotNull] Transformation transformation, [NotNull] string scriptName)
		{
			var scriptNameWithoutExtension = scriptName.EndsWith(".lua")
				? scriptName
				: $"{scriptName}.lua";

			return Path.Combine
			(
				this.BaseTransformationSpeciesPath,
				transformation.Species.Name,
				"Scripts",
				scriptNameWithoutExtension
			);
		}
	}
}
