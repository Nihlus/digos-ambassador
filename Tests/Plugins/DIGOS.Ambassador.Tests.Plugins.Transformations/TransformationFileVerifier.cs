//
//  TransformationFileVerifier.cs
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

using System.IO;
using System.Linq;
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using Remora.Results;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations;

/// <summary>
/// Verifies transformation content files.
/// </summary>
public class TransformationFileVerifier
{
    /// <summary>
    /// Verifies the content of the given file, treating it as a transformation file.
    /// </summary>
    /// <param name="file">The path to the file.</param>
    /// <typeparam name="T">The class to verify the file as.</typeparam>
    /// <returns>A condition result, which may or may not have succeeded.</returns>
    public Result VerifyFile<T>(string file)
    {
        using var sr = new StreamReader(File.OpenRead(file));
        var builder = new DeserializerBuilder()
            .WithTypeConverter(new ColourYamlConverter())
            .WithTypeConverter(new EnumYamlConverter<Pattern>())
            .WithTypeConverter(new NullableEnumYamlConverter<Pattern>())
            .WithNodeDeserializer(i => new ValidatingNodeDeserializer(i), s => s.InsteadOf<ObjectNodeDeserializer>())
            .WithNamingConvention(UnderscoredNamingConvention.Instance);

        if (typeof(T) != typeof(Species))
        {
            builder = builder.WithTypeConverter(new RawSpeciesYamlConverter());
        }

        var deserializer = builder.Build();

        var content = sr.ReadToEnd();

        try
        {
            deserializer.Deserialize<T>(content);
        }
        catch (YamlException yex)
        {
            return yex;
        }

        return Result.FromSuccess();
    }

    /// <summary>
    /// Verifies all yaml files in the given directory.
    /// </summary>
    /// <param name="directory">The directory to load files from.</param>
    /// <returns>A condition result, which may or may not have succeeded.</returns>
    public Result VerifyFilesInDirectory(string directory)
    {
        var files = Directory.EnumerateFiles(directory, "*.yml", SearchOption.AllDirectories).Where(p => !p.EndsWith("Species.yml")).ToList();

        if (files.Count <= 0)
        {
            return new NotFoundError("No files to verify in input directory.");
        }

        foreach (var file in files)
        {
            var verificationResult = VerifyFile<Transformation>(file);
            if (!verificationResult.IsSuccess)
            {
                return verificationResult;
            }
        }

        var speciesPath = Path.Combine(directory, "Species.yml");
        return !File.Exists(speciesPath)
            ? Result.FromSuccess()
            : VerifyFile<Species>(speciesPath);
    }
}
