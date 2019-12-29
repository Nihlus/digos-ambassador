//
//  ContentServiceExtensions.cs
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using DIGOS.Ambassador.Core.Async;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages;
using JetBrains.Annotations;
using Remora.Results;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;
using Zio;

namespace DIGOS.Ambassador.Plugins.Transformations.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="ContentService"/> class.
    /// </summary>
    internal static class ContentServiceExtensions
    {
        /// <summary>
        /// Gets the base dossier path.
        /// </summary>
        private static UPath BaseTransformationSpeciesPath { get; } = UPath.Combine
        (
            UPath.Root,
            "Transformations",
            "Species"
        );

        /// <summary>
        /// Gets the path to the transformation messages.
        /// </summary>
        private static UPath TransformationMessagesPath { get; } = UPath.Combine
        (
            UPath.Root,
            "Transformations",
            "messages.json"
        );

        /// <summary>
        /// Loads and retrieves the bundled transformation messages.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        public static RetrieveEntityResult<TransformationText> GetTransformationMessages
        (
            [NotNull] this ContentService @this
        )
        {
            if (!@this.FileSystem.FileExists(TransformationMessagesPath))
            {
                return RetrieveEntityResult<TransformationText>.FromError("Transformation messages not found.");
            }

            using var reader = new StreamReader
            (
                @this.FileSystem.OpenFile
                (
                    TransformationMessagesPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                )
            );

            var content = reader.ReadToEnd();
            if (!TransformationText.TryDeserialize(content, out var text))
            {
                return RetrieveEntityResult<TransformationText>.FromError("Failed to parse the messages.");
            }

            return RetrieveEntityResult<TransformationText>.FromSuccess(text);
        }

        /// <summary>
        /// Discovers the species that have been bundled with the program.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public static async Task<RetrieveEntityResult<IReadOnlyList<Species>>> DiscoverBundledSpeciesAsync
        (
            [NotNull] this ContentService @this
        )
        {
            const string speciesFilename = "Species.yml";

            var deser = new DeserializerBuilder()
                .WithNodeDeserializer(i => new ValidatingNodeDeserializer(i), s => s.InsteadOf<ObjectNodeDeserializer>())
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            var species = new List<Species>();
            var speciesFolders = @this.FileSystem.EnumerateDirectories
            (
                BaseTransformationSpeciesPath
            );

            foreach (var directory in speciesFolders)
            {
                var speciesFilePath = UPath.Combine(directory, speciesFilename);
                if (!@this.FileSystem.FileExists(speciesFilePath))
                {
                    continue;
                }

                var openStreamResult = @this.OpenLocalStream(speciesFilePath);
                if (!openStreamResult.IsSuccess)
                {
                    return RetrieveEntityResult<IReadOnlyList<Species>>.FromError(openStreamResult);
                }

                using var speciesFile = openStreamResult.Entity;
                var content = await AsyncIO.ReadAllTextAsync(speciesFile, Encoding.UTF8);

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
        /// <param name="this">The content service.</param>
        /// <param name="transformation">The transformation service.</param>
        /// <param name="species">The species to discover transformations for.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure, NotNull, ItemNotNull]
        public static async Task<RetrieveEntityResult<IReadOnlyList<Transformation>>> DiscoverBundledTransformationsAsync
        (
            [NotNull] this ContentService @this,
            [NotNull] TransformationService transformation,
            [NotNull] Species species
        )
        {
            const string speciesFilename = "Species.yml";

            var speciesDir = GetSpeciesDirectory(species);
            var transformationFiles = @this.FileSystem.EnumerateFiles(speciesDir)
                .Where(p => !p.ToString().EndsWith(speciesFilename));

            var transformations = new List<Transformation>();
            var deser = new DeserializerBuilder()
                .WithTypeConverter(new ColourYamlConverter())
                .WithTypeConverter(new SpeciesYamlConverter(transformation))
                .WithNodeDeserializer(i => new ValidatingNodeDeserializer(i), s => s.InsteadOf<ObjectNodeDeserializer>())
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            foreach (var transformationFile in transformationFiles)
            {
                var getTransformationFileStream = @this.OpenLocalStream(transformationFile);
                if (!getTransformationFileStream.IsSuccess)
                {
                    continue;
                }

                string content;
                using (var transformationFileStream = getTransformationFileStream.Entity)
                {
                    content = await AsyncIO.ReadAllTextAsync(transformationFileStream);
                }

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
        private static UPath GetSpeciesDirectory([NotNull] Species species)
        {
            return UPath.Combine(BaseTransformationSpeciesPath, species.Name);
        }

        /// <summary>
        /// Gets the absolute path to a named lua script belonging to the given transformation.
        /// </summary>
        /// <param name="transformation">The transformation that the script belongs to.</param>
        /// <param name="scriptName">The name of the script.</param>
        /// <returns>The path to the script.</returns>
        [Pure]
        public static UPath GetLuaScriptPath
        (
            [NotNull] Transformation transformation,
            [NotNull] string scriptName
        )
        {
            var scriptNameWithoutExtension = scriptName.EndsWith(".lua")
                ? scriptName
                : $"{scriptName}.lua";

            return UPath.Combine
            (
                BaseTransformationSpeciesPath,
                transformation.Species.Name,
                "Scripts",
                scriptNameWithoutExtension
            );
        }
    }
}
