//
//  TransformationValidityTestBase.cs
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
using DIGOS.Ambassador.Plugins.Transformations.Model;
using DIGOS.Ambassador.Plugins.Transformations.Model.Appearances;
using DIGOS.Ambassador.Plugins.Transformations.Transformations;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.NodeDeserializers;

namespace DIGOS.Ambassador.Tests.Plugins.Transformations
{
    /// <summary>
    /// Acts as a base for transformation validity tests.
    /// </summary>
    public class TransformationValidityTestBase
    {
        /// <summary>
        /// Gets an instance of a file verifier.
        /// </summary>
        protected TransformationFileVerifier Verifier { get; } = new TransformationFileVerifier();

        /// <summary>
        /// Deserializes a transformation object from the given file. The file is assumed to exist and be valid.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <typeparam name="T">The transformation object type.</typeparam>
        /// <returns>The object.</returns>
        protected T Deserialize<T>(string file)
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

            return deserializer.Deserialize<T>(content);
        }
    }
}
