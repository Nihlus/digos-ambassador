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
using DIGOS.Ambassador.Core.Results;
using DIGOS.Ambassador.Core.Services;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Services;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using JetBrains.Annotations;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace DIGOS.Ambassador.Plugins.Transformations.Extensions
{
    /// <summary>
    /// Extension methods for the <see cref="ContentService"/> class.
    /// </summary>
    public static class ContentServiceExtensions
    {
        /// <summary>
        /// Discovers the species that have been bundled with the program.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <returns>A retrieval result which may or may not have suceeded.</returns>
        [Pure]
        public static async Task<RetrieveEntityResult<IReadOnlyList<Species>>> DiscoverBundledSpeciesAsync
        (
            this ContentService @this
        )
        {
            const string speciesFilename = "Species.yml";

            var deser = new DeserializerBuilder()
                .WithNodeDeserializer(i => new ValidatingNodeDeserializer(i), s => s.InsteadOf<ObjectNodeDeserializer>())
                .WithNamingConvention(new UnderscoredNamingConvention())
                .Build();

            var species = new List<Species>();
            var speciesFolders = Directory.EnumerateDirectories(@this.BaseTransformationSpeciesPath);
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
        /// <param name="this">The content service.</param>
        /// <param name="db">The database.</param>
        /// <param name="transformation">The transformation service.</param>
        /// <param name="species">The species to discover transformations for.</param>
        /// <returns>A retrieval result which may or may not have succeeded.</returns>
        [Pure]
        public static async Task<RetrieveEntityResult<IReadOnlyList<Transformation>>> DiscoverBundledTransformationsAsync
        (
            this ContentService @this,
            [NotNull] TransformationsDatabaseContext db,
            [NotNull] TransformationService transformation,
            [NotNull] Species species
        )
        {
            const string speciesFilename = "Species.yml";

            var speciesDir = @this.GetSpeciesDirectory(species);
            var transformationFiles = Directory.EnumerateFiles(speciesDir).Where(p => !p.EndsWith(speciesFilename));

            var transformations = new List<Transformation>();
            var deser = new DeserializerBuilder()
                .WithTypeConverter(new ColourYamlConverter())
                .WithTypeConverter(new SpeciesYamlConverter(transformation))
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

        [NotNull]
        [Pure]
        private static string GetSpeciesDirectory(this ContentService @this, [NotNull] Species species)
        {
            return Path.Combine(@this.BaseTransformationSpeciesPath, species.Name);
        }

        /// <summary>
        /// Gets the absolute path to a named lua script belonging to the given transformation.
        /// </summary>
        /// <param name="this">The content service.</param>
        /// <param name="transformation">The transformation that the script belongs to.</param>
        /// <param name="scriptName">The name of the script.</param>
        /// <returns>The path to the script.</returns>
        [Pure]
        [NotNull]
        public static string GetLuaScriptPath(this ContentService @this, [NotNull] Transformation transformation, [NotNull] string scriptName)
        {
            var scriptNameWithoutExtension = scriptName.EndsWith(".lua")
                ? scriptName
                : $"{scriptName}.lua";

            return Path.Combine
            (
                @this.BaseTransformationSpeciesPath,
                transformation.Species.Name,
                "Scripts",
                scriptNameWithoutExtension
            );
        }
    }
}
