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
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using DIGOS.Ambassador.Plugins.Transformations.Transformations.Messages;
using JetBrains.Annotations;
using Remora.Commands.Results;
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
        public static Result<TransformationText> GetTransformationMessages
        (
            this ContentService @this
        )
        {
            if (!@this.FileSystem.FileExists(TransformationMessagesPath))
            {
                return new GenericError("Transformation messages not found.");
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
            return TransformationText.TryDeserialize(content, out var text)
                ? Result<TransformationText>.FromSuccess(text)
                : new ParsingError<TransformationText>("Failed to parse the messages.");
        }

        /// <summary>
        /// Discovers the species that have been bundled with the program.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public static async Task<Result<IReadOnlyList<Species>>> DiscoverBundledSpeciesAsync
        (
            this ContentService @this
        )
        {
            const string speciesFilename = "Species.yml";

            var deserializer = new DeserializerBuilder()
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
                    return Result<IReadOnlyList<Species>>.FromError(openStreamResult);
                }

                await using var speciesFile = openStreamResult.Entity;
                var content = await AsyncIO.ReadAllTextAsync(speciesFile, Encoding.UTF8);

                try
                {
                    species.Add(deserializer.Deserialize<Species>(content));
                }
                catch (YamlException yex)
                {
                    if (yex.InnerException is SerializationException sex)
                    {
                        return Result<IReadOnlyList<Species>>.FromError(sex);
                    }

                    return Result<IReadOnlyList<Species>>.FromError(yex);
                }
            }

            return Result<IReadOnlyList<Species>>.FromSuccess(species);
        }

        /// <summary>
        /// Discovers the transformations of a specific species that have been bundled with the program. The species
        /// must already be registered in the database.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <param name="transformation">The transformation service.</param>
        /// <param name="species">The species to discover transformations for.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public static async Task<Result<IReadOnlyList<Transformation>>> DiscoverBundledTransformationsAsync
        (
            this ContentService @this,
            TransformationService transformation,
            Species species
        )
        {
            const string speciesFilename = "Species.yml";

            var speciesDir = GetSpeciesDirectory(species);
            var transformationFiles = @this.FileSystem.EnumerateFiles(speciesDir)
                .Where(p => !p.ToString().EndsWith(speciesFilename));

            var transformations = new List<Transformation>();
            var deserializer = new DeserializerBuilder()
                .WithTypeConverter(new ColourYamlConverter())
                .WithTypeConverter(new SpeciesYamlConverter(transformation))
                .WithTypeConverter(new EnumYamlConverter<Pattern>())
                .WithTypeConverter(new NullableEnumYamlConverter<Pattern>())
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
                await using (var transformationFileStream = getTransformationFileStream.Entity)
                {
                    content = await AsyncIO.ReadAllTextAsync(transformationFileStream);
                }

                try
                {
                    transformations.Add(deserializer.Deserialize<Transformation>(content));
                }
                catch (YamlException yex)
                {
                    if (yex.InnerException is SerializationException sex)
                    {
                        return Result<IReadOnlyList<Transformation>>.FromError(sex);
                    }

                    return Result<IReadOnlyList<Transformation>>.FromError(yex);
                }
            }

            return Result<IReadOnlyList<Transformation>>.FromSuccess(transformations);
        }

        [Pure]
        private static UPath GetSpeciesDirectory(Species species)
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
            Transformation transformation,
            string scriptName
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
